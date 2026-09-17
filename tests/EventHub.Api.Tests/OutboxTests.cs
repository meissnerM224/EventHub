using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = $"outbox-{Guid.NewGuid():N}@test.de",
            Password = EventHubWebApplicationFactory.TestPassword,
            DisplayName = "Outbox Tester",
            Role = RoleName.Participant
        });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResult>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
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