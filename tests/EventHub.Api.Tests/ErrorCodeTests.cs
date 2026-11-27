using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using EventHub.Domain.Models;

namespace EventHub.Api.Tests;

/// <summary>
/// Every DomainException carries a machine-readable code that the frontend
/// maps to a translated message. These tests pin the code (not only the status)
/// for each error path reachable through the API.
/// </summary>
[Collection("EventHub")]
public sealed partial class ErrorCodeTests(EventHubWebApplicationFactory factory)
{
    // ── ErrorCodes ───────────────────────────────────────────────────────

    [Fact]
    public void ErrorCodes_AreUnique_AndFollowNamingConvention()
    {
        var codes = typeof(ErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(codes);
        Assert.Equal(codes.Count, codes.Distinct().Count());
        Assert.All(codes, code => Assert.Matches(CodePattern(), code));
    }

    // ── Events ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsEventNotFound_WhenEventDoesNotExist()
    {
        var id = Guid.NewGuid();
        var response = await factory.CreateClient().GetAsync($"/api/events/{id}");

        await AssertProblem(response, HttpStatusCode.NotFound, ErrorCodes.EventNotFound);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"Event '{id}' was not found.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task GetAll_ReturnsInvalidDates_WhenFromIsAfterTo()
    {
        var from = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(10).ToString("o"));
        var to = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(1).ToString("o"));

        var response = await factory.CreateClient().GetAsync($"/api/events?from={from}&to={to}");

        await AssertProblem(response, HttpStatusCode.BadRequest, ErrorCodes.EventInvalidDates);
    }

    [Fact]
    public async Task Create_ReturnsEventAlreadyExists_WhenEventIsDuplicated()
    {
        var organizer = await OrganizerClientAsync();
        var payload = NewEvent(UniqueTitle("Duplicate"));
        (await organizer.PostAsJsonAsync("/api/events", payload)).EnsureSuccessStatusCode();

        var response = await organizer.PostAsJsonAsync("/api/events", payload);

        await AssertProblem(response, HttpStatusCode.Conflict, ErrorCodes.EventAlreadyExists);
    }

    [Fact]
    public async Task Create_ReturnsCategoryNotFound_WhenCategoryDoesNotExist()
    {
        var organizer = await OrganizerClientAsync();

        var response = await organizer.PostAsJsonAsync(
            "/api/events", NewEvent(UniqueTitle("Unknown Category"), categoryId: 9999));

        await AssertProblem(response, HttpStatusCode.NotFound, ErrorCodes.CategoryNotFound);
    }

    [Fact]
    public async Task Create_ReturnsInvalidDates_WhenDoorsOpenAfterStart()
    {
        var organizer = await OrganizerClientAsync();

        var response = await organizer.PostAsJsonAsync("/api/events", NewEvent(
            UniqueTitle("Doors After Start"),
            startAt: DateTimeOffset.UtcNow.AddHours(2),
            doorsOpenAt: DateTimeOffset.UtcNow.AddHours(3)));

        await AssertProblem(response, HttpStatusCode.BadRequest, ErrorCodes.EventInvalidDates);
    }

    [Fact]
    public async Task Create_ReturnsInvalidDates_WhenStartIsInThePast()
    {
        var organizer = await OrganizerClientAsync();

        var response = await organizer.PostAsJsonAsync("/api/events", NewEvent(
            UniqueTitle("Past Start"),
            startAt: DateTimeOffset.UtcNow.AddHours(-1),
            doorsOpenAt: DateTimeOffset.UtcNow.AddHours(-2)));

        await AssertProblem(response, HttpStatusCode.BadRequest, ErrorCodes.EventInvalidDates);
    }

    [Fact]
    public async Task Create_ReturnsInvalidImage_WhenImageUrlIsExternal()
    {
        var organizer = await OrganizerClientAsync();

        var response = await organizer.PostAsJsonAsync("/api/events",
            NewEvent(UniqueTitle("External Image"), image: "https://example.com/tracker.gif"));

        await AssertProblem(response, HttpStatusCode.BadRequest, ErrorCodes.EventInvalidImage);
    }

    [Fact]
    public async Task Update_ReturnsInvalidDates_WhenDoorsOpenAfterStart()
    {
        var organizer = await OrganizerClientAsync();
        var eventId = await CreateEventAsync(organizer, UniqueTitle("Update Doors"));

        var response = await organizer.PutAsJsonAsync($"/api/events/{eventId}", NewEvent(
            UniqueTitle("Update Doors"),
            startAt: DateTimeOffset.UtcNow.AddHours(2),
            doorsOpenAt: DateTimeOffset.UtcNow.AddHours(3)));

        await AssertProblem(response, HttpStatusCode.BadRequest, ErrorCodes.EventInvalidDates);
    }

    [Fact]
    public async Task Update_ReturnsInvalidDates_WhenStartIsInThePast()
    {
        var organizer = await OrganizerClientAsync();
        var eventId = await CreateEventAsync(organizer, UniqueTitle("Update Past"));

        var response = await organizer.PutAsJsonAsync($"/api/events/{eventId}", NewEvent(
            UniqueTitle("Update Past"),
            startAt: DateTimeOffset.UtcNow.AddHours(-1),
            doorsOpenAt: DateTimeOffset.UtcNow.AddHours(-2)));

        await AssertProblem(response, HttpStatusCode.BadRequest, ErrorCodes.EventInvalidDates);
    }

    [Fact]
    public async Task Update_ReturnsNotOwner_WhenUserIsNotTheOrganizer()
    {
        var eventId = await CreateEventAsync(await OrganizerClientAsync(), UniqueTitle("Owned"));
        var stranger = await OtherOrganizerClientAsync();

        var response = await stranger.PutAsJsonAsync(
            $"/api/events/{eventId}", NewEvent(UniqueTitle("Hijacked")));

        await AssertProblem(response, HttpStatusCode.Forbidden, ErrorCodes.EventNotOwner);
    }

    [Fact]
    public async Task Update_ReturnsNotOwner_EvenWhenTitleCollidesWithAnotherEvent()
    {
        // Ownership is checked first, so a stranger can't probe for existing events via 409.
        var organizer = await OrganizerClientAsync();
        var takenTitle = UniqueTitle("Taken");
        await CreateEventAsync(organizer, takenTitle);
        var eventId = await CreateEventAsync(organizer, UniqueTitle("Target"));
        var stranger = await OtherOrganizerClientAsync();

        var response = await stranger.PutAsJsonAsync($"/api/events/{eventId}", NewEvent(takenTitle));

        await AssertProblem(response, HttpStatusCode.Forbidden, ErrorCodes.EventNotOwner);
    }

    [Fact]
    public async Task Update_ReturnsEventCancelled_WhenEventWasCancelled()
    {
        var organizer = await OrganizerClientAsync();
        var title = UniqueTitle("Cancelled Update");
        var eventId = await CreateEventAsync(organizer, title);
        (await organizer.DeleteAsync($"/api/events/{eventId}")).EnsureSuccessStatusCode();

        var response = await organizer.PutAsJsonAsync($"/api/events/{eventId}", NewEvent(title));

        await AssertProblem(response, HttpStatusCode.BadRequest, ErrorCodes.EventCancelled);
    }

    [Fact]
    public async Task Update_ReturnsCapacityTooLow_WhenMaxParticipantsIsBelowBookings()
    {
        var organizer = await OrganizerClientAsync();
        var title = UniqueTitle("Shrink");
        var eventId = await CreateEventAsync(organizer, title, maxParticipants: 5);
        foreach (var _ in Enumerable.Range(0, 2))
            (await (await RegisterParticipantAsync()).PostAsync($"/api/events/{eventId}/bookings", null))
                .EnsureSuccessStatusCode();

        var tooLow = await organizer.PutAsJsonAsync(
            $"/api/events/{eventId}", NewEvent(title, maxParticipants: 1));
        var exact = await organizer.PutAsJsonAsync(
            $"/api/events/{eventId}", NewEvent(title, maxParticipants: 2));

        await AssertProblem(tooLow, HttpStatusCode.BadRequest, ErrorCodes.EventCapacityTooLow);
        Assert.True(exact.IsSuccessStatusCode, await exact.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cancel_ReturnsNotOwner_WhenUserIsNotTheOrganizer()
    {
        var eventId = await CreateEventAsync(await OrganizerClientAsync(), UniqueTitle("Not Yours To Cancel"));
        var stranger = await OtherOrganizerClientAsync();

        var response = await stranger.DeleteAsync($"/api/events/{eventId}");

        await AssertProblem(response, HttpStatusCode.Forbidden, ErrorCodes.EventNotOwner);
    }

    // ── Bookings ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBookings_ReturnsNotOwner_WhenUserIsNotTheOrganizer()
    {
        var eventId = await CreateEventAsync(await OrganizerClientAsync(), UniqueTitle("Guest List"));
        var stranger = await OtherOrganizerClientAsync();

        var response = await stranger.GetAsync($"/api/events/{eventId}/bookings");

        await AssertProblem(response, HttpStatusCode.Forbidden, ErrorCodes.EventNotOwner);
    }

    [Fact]
    public async Task Book_ReturnsEventNotFound_WhenEventDoesNotExist()
    {
        var participant = await RegisterParticipantAsync();

        var response = await participant.PostAsync($"/api/events/{Guid.NewGuid()}/bookings", null);

        await AssertProblem(response, HttpStatusCode.NotFound, ErrorCodes.EventNotFound);
    }

    [Fact]
    public async Task Book_ReturnsFullyBooked_WhenNoSpotIsLeft()
    {
        var eventId = await CreateEventAsync(
            await OrganizerClientAsync(), UniqueTitle("Full"), maxParticipants: 1);
        (await (await RegisterParticipantAsync()).PostAsync($"/api/events/{eventId}/bookings", null))
            .EnsureSuccessStatusCode();

        var response = await (await RegisterParticipantAsync())
            .PostAsync($"/api/events/{eventId}/bookings", null);

        await AssertProblem(response, HttpStatusCode.Conflict, ErrorCodes.EventFullyBooked);
    }

    [Fact]
    public async Task Book_ReturnsBookingAlreadyExists_WhenUserBooksTwice()
    {
        var eventId = await CreateEventAsync(await OrganizerClientAsync(), UniqueTitle("Book Twice"));
        var participant = await RegisterParticipantAsync();
        (await participant.PostAsync($"/api/events/{eventId}/bookings", null)).EnsureSuccessStatusCode();

        var response = await participant.PostAsync($"/api/events/{eventId}/bookings", null);

        await AssertProblem(response, HttpStatusCode.Conflict, ErrorCodes.BookingAlreadyExists);
    }

    [Fact]
    public async Task Book_ReturnsEventCancelled_WhenEventWasCancelled()
    {
        var organizer = await OrganizerClientAsync();
        var eventId = await CreateEventAsync(organizer, UniqueTitle("Cancelled"));
        (await organizer.DeleteAsync($"/api/events/{eventId}")).EnsureSuccessStatusCode();

        var response = await (await RegisterParticipantAsync())
            .PostAsync($"/api/events/{eventId}/bookings", null);

        await AssertProblem(response, HttpStatusCode.BadRequest, ErrorCodes.EventCancelled);
    }

    [Fact]
    public async Task CancelBooking_ReturnsBookingNotFound_WhenUserHasNoBooking()
    {
        var eventId = await CreateEventAsync(await OrganizerClientAsync(), UniqueTitle("Never Booked"));
        var participant = await RegisterParticipantAsync();

        var response = await participant.DeleteAsync($"/api/events/{eventId}/bookings/me");

        await AssertProblem(response, HttpStatusCode.NotFound, ErrorCodes.BookingNotFound);
    }

    [Fact]
    public async Task Book_ReturnsEventAlreadyStarted_AfterStart()
    {
        var eventId = await CreateEventStartingSoonAsync(UniqueTitle("Started Book"));
        var participant = await RegisterParticipantAsync();

        using (factory.Clock.Advance(TimeSpan.FromMinutes(5)))
        {
            var response = await participant.PostAsync($"/api/events/{eventId}/bookings", null);

            await AssertProblem(response, HttpStatusCode.BadRequest, ErrorCodes.EventAlreadyStarted);
        }
    }

    [Fact]
    public async Task CancelBooking_ReturnsEventAlreadyStarted_AfterStart()
    {
        var eventId = await CreateEventStartingSoonAsync(UniqueTitle("Started Cancel"));
        var participant = await RegisterParticipantAsync();
        (await participant.PostAsync($"/api/events/{eventId}/bookings", null)).EnsureSuccessStatusCode();

        using (factory.Clock.Advance(TimeSpan.FromMinutes(5)))
        {
            var response = await participant.DeleteAsync($"/api/events/{eventId}/bookings/me");

            await AssertProblem(response, HttpStatusCode.BadRequest, ErrorCodes.EventAlreadyStarted);
        }
    }

    // Starts in 2 minutes – only a small clock shift is needed, so login tokens stay valid.
    private async Task<Guid> CreateEventStartingSoonAsync(string title)
    {
        var now = factory.Clock.GetUtcNow();
        var response = await (await OrganizerClientAsync()).PostAsJsonAsync("/api/events",
            NewEvent(title, startAt: now.AddMinutes(2), doorsOpenAt: now.AddMinutes(1)));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Create event '{title}' failed: {body}");
        return Guid.Parse(response.Headers.Location!.Segments.Last());
    }

    // ── Auth ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ReturnsInvalidCredentials_WhenPasswordIsWrong()
    {
        var response = await factory.CreateHttpsClient().PostAsJsonAsync("/api/auth/login", new
        {
            Email = EventHubWebApplicationFactory.ParticipantEmail,
            Password = "Wrong1234!"
        });

        await AssertProblem(response, HttpStatusCode.Unauthorized, ErrorCodes.AuthInvalidCredentials);
    }

    [Fact]
    public async Task Register_ReturnsEmailTaken_WhenEmailIsAlreadyRegistered()
    {
        var response = await factory.CreateHttpsClient().PostAsJsonAsync("/api/auth/register", new
        {
            Email = EventHubWebApplicationFactory.ParticipantEmail,
            Password = EventHubWebApplicationFactory.TestPassword,
            DisplayName = "Copycat"
        });

        await AssertProblem(response, HttpStatusCode.Conflict, ErrorCodes.UserEmailTaken);
    }

    [Fact]
    public async Task Register_ReturnsRegistrationFailed_WhenIdentityRejectsPassword()
    {
        // Passes model validation (8+ chars) but fails Identity's digit/uppercase rules.
        var response = await factory.CreateHttpsClient().PostAsJsonAsync("/api/auth/register", new
        {
            Email = $"weak-{Guid.NewGuid():N}@test.de",
            Password = "abcdefgh",
            DisplayName = "Weak"
        });

        await AssertProblem(response, HttpStatusCode.BadRequest, ErrorCodes.UserRegistrationFailed);
    }

    // ── Admin ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GrantOrganizer_ReturnsUserNotFound_WhenUserDoesNotExist()
    {
        var admin = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.AdminEmail);

        var response = await admin.PutAsync($"/api/admin/users/{Guid.NewGuid()}/roles/organizer", null);

        await AssertProblem(response, HttpStatusCode.NotFound, ErrorCodes.UserNotFound);
    }

    // ── Upload ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_ReturnsFileUnsupported_WhenFileIsNotAnImage()
    {
        var organizer = await OrganizerClientAsync();
        var bytes = new ByteArrayContent([.. "kein bild"u8]);
        bytes.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        var content = new MultipartFormDataContent { { bytes, "file", "picture.jpg" } };

        var response = await organizer.PostAsync("/api/uploads/images", content);

        await AssertProblem(response, HttpStatusCode.BadRequest, ErrorCodes.FileUnsupported);
    }

    [Fact]
    public async Task Upload_ReturnsFileMissing_WhenFileIsEmpty()
    {
        var organizer = await OrganizerClientAsync();
        var empty = new ByteArrayContent([]);
        empty.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var content = new MultipartFormDataContent { { empty, "file", "empty.png" } };

        var response = await organizer.PostAsync("/api/uploads/images", content);

        await AssertProblem(response, HttpStatusCode.BadRequest, ErrorCodes.FileMissing);
    }

    // ── Helper ───────────────────────────────────────────────────────────

    [GeneratedRegex(@"^[a-z]+(\.[a-z_]+)+$")]
    private static partial Regex CodePattern();

    private static string UniqueTitle(string title) => $"{title} {Guid.NewGuid():N}";

    private Task<HttpClient> OrganizerClientAsync() =>
        factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);

    private Task<HttpClient> OtherOrganizerClientAsync() =>
        factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OtherOrganizerEmail);

    private async Task<HttpClient> RegisterParticipantAsync()
    {
        var client = factory.CreateHttpsClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = $"codes-{Guid.NewGuid():N}@test.de",
            Password = EventHubWebApplicationFactory.TestPassword,
            DisplayName = "Participant"
        });
        response.EnsureSuccessStatusCode();
        return client;
    }

    private static object NewEvent(
        string title,
        string? image = null,
        int maxParticipants = 10,
        int categoryId = EventHubWebApplicationFactory.FirstCategoryId,
        DateTimeOffset? startAt = null,
        DateTimeOffset? doorsOpenAt = null) => new
        {
            title,
            description = "Test description",
            image,
            location = "Theater Kassel",
            startAt = startAt ?? DateTimeOffset.UtcNow.AddHours(2),
            doorsOpenAt = doorsOpenAt ?? DateTimeOffset.UtcNow.AddHours(1),
            maxParticipants,
            categoryId
        };

    private static async Task<Guid> CreateEventAsync(HttpClient organizer, string title, int maxParticipants = 10)
    {
        var response = await organizer.PostAsJsonAsync(
            "/api/events", NewEvent(title, maxParticipants: maxParticipants));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Create event '{title}' failed: {body}");
        return Guid.Parse(response.Headers.Location!.Segments.Last());
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status,
            $"Expected {(int)status}, got {(int)response.StatusCode}: {body}");

        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("code", out var actual), $"No 'code' in: {body}");
        Assert.Equal(code, actual.GetString());
    }
}
