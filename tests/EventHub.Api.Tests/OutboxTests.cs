using System.Net;
using System.Net.Http.Json;
using EventHub.Api.Models;
using EventHub.Domain.Authorization;
using EventHub.Domain.Messaging;
using EventHub.Domain.Models;
using EventHub.Infrastructure.Entities;
using EventHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.Tests;

[Collection("EventHub")]
public sealed class OutboxTests(EventHubWebApplicationFactory factory)

{
    [Fact]
    public async Task Book_WritesOutboxMessage_WhenBookingSucceeds()
    {
        var eventId = await CreateEventAsync("Outbox Happy Path");
        var participant = await CreateParticipantClientAsync();

        var response = await participant.PostAsync($"/api/events/{eventId}/bookings", content: null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var confirmation = await response.Content.ReadFromJsonAsync<BookingConfirmation>();

        var messages = await ReadOutboxAsync();
        var message = Assert.Single(messages, m => m.Payload.Contains(confirmation!.BookingId.ToString()));

        Assert.Equal(nameof(BookingConfirmationMessage), message.Type);
    }

    [Fact]
    public async Task Book_WritesNoOutboxMessage_WhenBookingIsRejected()
    {
        var eventId = await CreateEventAsync("Outbox Rejected", maxParticipants: 1);

        var first = await CreateParticipantClientAsync();
        (await first.PostAsync($"/api/events/{eventId}/bookings", content: null))
            .EnsureSuccessStatusCode();

        var before = (await ReadOutboxAsync()).Count;

        var second = await CreateParticipantClientAsync();
        var response = await second.PostAsync($"/api/events/{eventId}/bookings", content: null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var after = (await ReadOutboxAsync()).Count;
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Book_EventuallySendsConfirmation()
    {
        var eventId = await CreateEventAsync("Outbox Delivery");
        var participant = await CreateParticipantClientAsync();

        var response = await participant.PostAsync($"/api/events/{eventId}/bookings", content: null);
        response.EnsureSuccessStatusCode();
        var confirmation = await response.Content.ReadFromJsonAsync<BookingConfirmation>();

        var sent = await WaitForAsync(
            () => factory.Notifications.Sent
                .FirstOrDefault(m => m.BookingId == confirmation!.BookingId),
            TimeSpan.FromSeconds(5));

        Assert.NotNull(sent);
    }


    // ── Failure path ─────────────────────────────────────────────────────

    [Fact]
    public async Task Worker_RetriesAfterFailure_AndClearsTheError()
    {
        var eventId = await CreateEventAsync("Outbox Retry");
        var (participant, userId) = await CreateParticipantAsync();
        factory.Notifications.FailFor(userId, times: 2);

        (await participant.PostAsync($"/api/events/{eventId}/bookings", null)).EnsureSuccessStatusCode();

        var processed = await WaitForAsync(async () =>
            (await OutboxFor(userId)) is { ProcessedAt: not null } m ? m : null);

        Assert.NotNull(processed);
        Assert.Equal(3, processed.Attempts);
        Assert.Null(processed.LastError);
        Assert.Single(factory.Notifications.Sent, m => m.UserId == userId);
    }

    [Fact]
    public async Task Worker_StopsAfterMaxAttempts_KeepsTheError_AndDoesNotBlockOthers()
    {
        var eventId = await CreateEventAsync("Outbox Give Up");
        var (failing, failingUser) = await CreateParticipantAsync();
        var (healthy, healthyUser) = await CreateParticipantAsync();
        factory.Notifications.FailFor(failingUser);

        (await failing.PostAsync($"/api/events/{eventId}/bookings", null)).EnsureSuccessStatusCode();
        (await healthy.PostAsync($"/api/events/{eventId}/bookings", null)).EnsureSuccessStatusCode();

        var givenUp = await WaitForAsync(async () =>
            (await OutboxFor(failingUser)) is { Attempts: MaxAttempts } m ? m : null);

        Assert.NotNull(givenUp);
        Assert.Null(givenUp.ProcessedAt);
        Assert.Equal($"Mail server unavailable for {failingUser}", givenUp.LastError);

        // A failing message must not hold up the rest of the batch.
        Assert.NotNull(await WaitForAsync(() =>
            Task.FromResult(factory.Notifications.Sent.FirstOrDefault(m => m.UserId == healthyUser))));

        // Several more poll intervals (200 ms): no further attempt after the limit.
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Equal(MaxAttempts, factory.Notifications.CallsFor(failingUser));
        Assert.Equal(MaxAttempts, (await OutboxFor(failingUser))!.Attempts);
    }

    // OutboxOptions.MaxAttempts default – the test factory does not override it.
    private const int MaxAttempts = 5;

    private async Task<OutboxMessage?> OutboxFor(Guid userId) =>
        (await ReadOutboxAsync()).SingleOrDefault(m => m.Payload.Contains(userId.ToString()));

    private async Task<(HttpClient Client, Guid UserId)> CreateParticipantAsync()
    {
        var client = await CreateParticipantClientAsync();
        var me = await client.GetFromJsonAsync<CurrentUser>("/api/auth/me");
        return (client, me!.UserId);
    }

    private static async Task<T?> WaitForAsync<T>(Func<Task<T?>> probe) where T : class
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            if (await probe() is { } result) return result;
            await Task.Delay(50);
        }

        return null;
    }

    private async Task<List<OutboxMessage>> ReadOutboxAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<EventHubDbContext>();
        return await context.Set<OutboxMessage>().AsNoTracking().ToListAsync();
    }

    private async Task<Guid> CreateEventAsync(string title, int maxParticipants = 10)
    {
        var organizer = await factory.CreateAuthenticatedClientAsync(
            EventHubWebApplicationFactory.OrganizerEmail);

        var response = await organizer.PostAsJsonAsync("/api/events", new
        {
            title,
            description = "Test description",
            location = "Theater Kassel",
            startAt = DateTimeOffset.UtcNow.AddHours(2),
            doorsOpenAt = DateTimeOffset.UtcNow.AddHours(1),
            maxParticipants,
            categoryId = EventHubWebApplicationFactory.FirstCategoryId
        });

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Create event '{title}' failed: {(int)response.StatusCode}\n{body}");
        }

        return Guid.Parse(response.Headers.Location!.Segments.Last());
    }

    private async Task<HttpClient> CreateParticipantClientAsync()
    {
        var client = factory.CreateHttpsClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = $"outbox-{Guid.NewGuid():N}@test.de",
            Password = EventHubWebApplicationFactory.TestPassword,
            DisplayName = "Outbox Tester",
            Role = RoleName.Participant
        });
        response.EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<T?> WaitForAsync<T>(Func<T?> probe, TimeSpan timeout) where T : class
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (probe() is { } result) return result;
            await Task.Delay(50);
        }

        return null;
    }
}