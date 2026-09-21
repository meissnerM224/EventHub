using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using EventHub.Api.Models;
using EventHub.Domain.Authorization;
using EventHub.Domain.Models;
using EventHub.Domain.Storage;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace EventHub.Api.Tests;

[Collection("EventHub")]
public sealed class EndpointTests(EventHubWebApplicationFactory factory)
{
    // Helper
    private async Task<HttpClient> CreateParticipantClientAsync()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = $"race-{Guid.NewGuid():N}@test.de",
            Password = EventHubWebApplicationFactory.TestPassword,
            DisplayName = "Racer",
            Role = RoleName.Participant
        });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResult>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private static MultipartFormDataContent ImageContent()
    {
        using var image = new Image<Rgba32>(40, 30);
        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());

        var file = new ByteArrayContent(ms.ToArray());
        file.Headers.ContentType = new("image/png")
        {
            CharSet = null
        };

        return new MultipartFormDataContent { { file, "file", "test.png" } };
    }


    private static object NewEvent(
        string title,
        string? image = null,
        int maxParticipants = 1000,
        int categoryId = 1,
        DateTimeOffset? startAt = null,
        DateTimeOffset? doorsOpenAt = null)
    {
        return new
        {
            title,
            description = "Test description",
            Image = image,
            location = "Theater Kassel",
            startAt = startAt ?? DateTimeOffset.UtcNow.AddHours(2),
            doorsOpenAt = doorsOpenAt ?? DateTimeOffset.UtcNow.AddHours(1),
            maxParticipants,
            categoryId
        };
    }


    private async Task<Guid> CreateEventAsync(
        string title,
        string? image = null,
        int maxParticipants = 10,
        int categoryId = 1,
        DateTimeOffset? startAt = null,
        DateTimeOffset? doorsOpenAt = null)
    {
        var organizer = await factory.CreateAuthenticatedClientAsync(
            EventHubWebApplicationFactory.OrganizerEmail);
        var response =
            await organizer.PostAsJsonAsync(
                "/api/events",
                NewEvent(title: title,
                    image: image,
                    maxParticipants: maxParticipants,
                    startAt: startAt ?? DateTimeOffset.UtcNow.AddHours(2),
                    doorsOpenAt: doorsOpenAt ?? DateTimeOffset.UtcNow.AddHours(1),
                    categoryId: categoryId
                ));
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Create event '{title}' failed: {(int)response.StatusCode}\n{body}");
        }

        return Guid.Parse(response.Headers.Location!.Segments.Last());
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var response = await factory.CreateClient().GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_FiltersByCategory()
    {
        await CreateEventAsync("Cat Filter A", categoryId: EventHubWebApplicationFactory.FirstCategoryId);
        await CreateEventAsync("Cat Filter B", categoryId: EventHubWebApplicationFactory.SecondCategoryId);

        var client = factory.CreateClient();
        var events = await client.GetFromJsonAsync<List<EventSummary>>("/api/events?categoryId=1");

        Assert.All(events!, e => Assert.Equal(EventHubWebApplicationFactory.FirstCategoryName, e.CategoryName));
        Assert.Contains(events!, e => e.Title == "Cat Filter A");
        Assert.DoesNotContain(events!, e => e.Title == "Cat Filter B");
    }

    [Fact]
    public async Task GetAll_ReturnsBadRequest_WhenFromIsAfterTo()
    {
        var from = DateTimeOffset.UtcNow.AddDays(10).ToString("o");
        var to = DateTimeOffset.UtcNow.AddDays(1).ToString("o");

        var response = await factory.CreateClient().GetAsync(
            $"/api/events?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_FiltersByDateRange()
    {
        var from = DateTimeOffset.UtcNow.AddDays(30);
        await CreateEventAsync("Far Future", startAt: from.AddDays(5));
        await CreateEventAsync("Near Future", startAt: DateTimeOffset.UtcNow.AddDays(1));

        var events = await factory.CreateClient()
            .GetFromJsonAsync<List<EventSummary>>($"/api/events?from={Uri.EscapeDataString(from.ToString("o"))}");

        Assert.Contains(events!, e => e.Title == "Far Future");
        Assert.DoesNotContain(events!, e => e.Title == "Near Future");
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenEventExists()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);

        var postResponse = await client.PostAsJsonAsync("/api/events", NewEvent("Happy Path"));
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var location = postResponse.Headers.Location;
        Assert.NotNull(location);

        var getResponse = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenEventDoesNotExist()
    {
        var response = await factory.CreateClient().GetAsync($"/api/events/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenIdIsNotAGuid()
    {
        var response = await factory.CreateClient().GetAsync("/api/events/wadnlwadawdaq231");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenBodyIsEmpty()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);
        var response = await client.PostAsync(
            "/api/events",
            new StringContent("", Encoding.UTF8, "application/json")
        );
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenEventAlreadyExists()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);
        var payload = NewEvent("Conflict Test");
        await client.PostAsJsonAsync("/api/events", payload);

        var response = await client.PostAsJsonAsync("/api/events", payload);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenEventExists()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);

        var postResponse = await client.PostAsJsonAsync("/api/events", NewEvent("Update Happy Path"));
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var location = postResponse.Headers.Location;
        Assert.NotNull(location);

        var updated = new
        {
            title = "Changed Title",
            description = "New Description",
            location = "Culture center Kassel",
            startAt = DateTimeOffset.UtcNow.AddHours(5),
            doorsOpenAt = DateTimeOffset.UtcNow.AddHours(4),
            maxParticipants = 500,
            categoryId = 1
        };

        var putResponse = await client.PutAsJsonAsync(location, updated);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var getResponse = await client.GetAsync(location);
        var detail = await getResponse.Content.ReadFromJsonAsync<EventDetail>();
        Assert.Equal("Changed Title", detail!.EventTitle);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenEventDoesNotExist()
    {
        var updated = new
        {
            title = "Any",
            description = "Any",
            location = "Any",
            startAt = DateTimeOffset.UtcNow.AddHours(5),
            doorsOpenAt = DateTimeOffset.UtcNow.AddHours(4),
            maxParticipants = 100,
            categoryId = 1
        };

        const string email = EventHubWebApplicationFactory.OrganizerEmail;
        var client = await factory.CreateAuthenticatedClientAsync(email);
        var response = await client.PutAsJsonAsync($"/api/events/{Guid.NewGuid()}", updated);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenStartAtIsInThePast()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);

        var postResponse = await client.PostAsJsonAsync("/api/events", NewEvent("Update past"));
        var location = postResponse.Headers.Location;

        var updated = new
        {
            title = "Title",
            description = "Description",
            location = "Kassel",
            startAt = DateTimeOffset.UtcNow.AddHours(-5),
            doorsOpenAt = DateTimeOffset.UtcNow.AddHours(-6),
            maxParticipants = 100,
            categoryId = 1
        };

        var response = await client.PutAsJsonAsync(location, updated);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenEventExists()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);
        var postResponse = await client.PostAsJsonAsync("/api/events", NewEvent("Delete Test"));
        var location = postResponse.Headers.Location;

        var deleteResponse = await client.DeleteAsync(location);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenEventDoesNotExist()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);
        var response = await client.DeleteAsync($"/api/events/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_IsIdempotent()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);
        var postResponse = await client.PostAsJsonAsync("/api/events", NewEvent("Delete Twice"));
        var location = postResponse.Headers.Location;

        await client.DeleteAsync(location);
        var second = await client.DeleteAsync(location);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WhenNoTokenIsProvided()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/events", NewEvent("No Token"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsForbidden_WhenUserIsNotAnOrganizer()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.ParticipantEmail);
        var response = await client.PostAsJsonAsync("/api/events", NewEvent("Wrong Role"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenEmailIsUnknown()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new
        {
            Email = "unknown@test.de",
            Password = EventHubWebApplicationFactory.TestPassword
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }


    [Fact]
    public async Task Update_ReturnsForbidden_WhenUserIsNotTheOrganizer()
    {
        var owner = await factory.CreateAuthenticatedClientAsync(
            EventHubWebApplicationFactory.OrganizerEmail);
        var created = await owner.PostAsJsonAsync("/api/events", NewEvent("Owned Event"));
        var location = created.Headers.Location!;

        var stranger = await factory.CreateAuthenticatedClientAsync(
            EventHubWebApplicationFactory.OtherOrganizerEmail);

        var response = await stranger.PutAsJsonAsync(location, NewEvent("Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Book_AllowsExactlyOneBooking_WhenLastSpotIsContested()
    {
        const int contenders = 20;

        var organizer = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);
        var create = await organizer.PostAsJsonAsync("/api/events", NewEvent("Last Place", maxParticipants: 1));
        create.EnsureSuccessStatusCode();
        var eventId = Guid.Parse(create.Headers.Location!.Segments.Last());
        var clients = await Task.WhenAll(Enumerable.Range(0, contenders).Select(_ => CreateParticipantClientAsync()));
        var responses = await Task.WhenAll(clients.Select(c =>
            c.PostAsync($"/api/events/{eventId}/bookings", content: null)));

        var codes = responses.Select(r => r.StatusCode).ToList();
        Assert.Equal(1, codes.Count(c => c == HttpStatusCode.Created));
        Assert.Equal(contenders - 1, codes.Count(c => c == HttpStatusCode.Conflict));

        var detail = await factory.CreateClient().GetFromJsonAsync<EventDetail>($"/api/events/{eventId}");
        Assert.Equal(0, detail!.AvailableSpots);
    }

    [Fact]
    public async Task Book_ReturnsConflict_WhenEventIsFullyBooked()
    {
        var organizer = await factory.CreateAuthenticatedClientAsync(
            EventHubWebApplicationFactory.OrganizerEmail);
        var create = await organizer.PostAsJsonAsync("/api/events", NewEvent("Full Event", maxParticipants: 1));
        create.EnsureSuccessStatusCode();
        var eventId = Guid.Parse(create.Headers.Location!.Segments.Last());

        var first = await CreateParticipantClientAsync();
        var second = await CreateParticipantClientAsync();

        var firstResponse = await first.PostAsync($"/api/events/{eventId}/bookings", content: null);
        var secondResponse = await second.PostAsync($"/api/events/{eventId}/bookings", content: null);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Book_ReturnsCreated_WhenSpotWasFreedByCancellation()
    {
        var organizer = await factory.CreateAuthenticatedClientAsync(
            EventHubWebApplicationFactory.OrganizerEmail);
        var create = await organizer.PostAsJsonAsync("/api/events", NewEvent("Freed Spot", maxParticipants: 1));
        create.EnsureSuccessStatusCode();
        var eventId = Guid.Parse(create.Headers.Location!.Segments.Last());

        var first = await CreateParticipantClientAsync();
        var second = await CreateParticipantClientAsync();

        (await first.PostAsync($"/api/events/{eventId}/bookings", content: null)).EnsureSuccessStatusCode();
        var cancel = await first.DeleteAsync($"/api/events/{eventId}/bookings/me");
        var rebook = await second.PostAsync($"/api/events/{eventId}/bookings", content: null);

        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        Assert.Equal(HttpStatusCode.Created, rebook.StatusCode);
    }


    [Fact]
    public async Task CancelBooking_IsIdempotent()
    {
        var eventId = await CreateEventAsync("Cancel Twice");
        var participant = await CreateParticipantClientAsync();
        (await participant.PostAsync($"/api/events/{eventId}/bookings", content: null)).EnsureSuccessStatusCode();

        var first = await participant.DeleteAsync($"/api/events/{eventId}/bookings/me");
        var second = await participant.DeleteAsync($"/api/events/{eventId}/bookings/me");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_ReturnsNotFound_WhenUserHasNoBooking()
    {
        var eventId = await CreateEventAsync("Never Booked");
        var participant = await CreateParticipantClientAsync();

        var response = await participant.DeleteAsync($"/api/events/{eventId}/bookings/me");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBookings_ReturnsParticipants_ForOrganizer()
    {
        var eventId = await CreateEventAsync("Guest List");
        var participant = await CreateParticipantClientAsync();
        (await participant.PostAsync($"/api/events/{eventId}/bookings", content: null)).EnsureSuccessStatusCode();

        var organizer = await factory.CreateAuthenticatedClientAsync(
            EventHubWebApplicationFactory.OrganizerEmail);
        var bookings = await organizer.GetFromJsonAsync<List<BookingSummary>>($"/api/events/{eventId}/bookings");

        Assert.Single(bookings!);
    }

    [Fact]
    public async Task GetBookings_ExcludesCancelledBookings()
    {
        var eventId = await CreateEventAsync("Cancelled Guest");
        var participant = await CreateParticipantClientAsync();
        (await participant.PostAsync($"/api/events/{eventId}/bookings", content: null)).EnsureSuccessStatusCode();
        (await participant.DeleteAsync($"/api/events/{eventId}/bookings/me")).EnsureSuccessStatusCode();

        var organizer = await factory.CreateAuthenticatedClientAsync(
            EventHubWebApplicationFactory.OrganizerEmail);
        var bookings = await organizer.GetFromJsonAsync<List<BookingSummary>>($"/api/events/{eventId}/bookings");

        Assert.Empty(bookings!);
    }

    [Fact]
    public async Task GetBookings_ReturnsForbidden_WhenUserIsNotTheOrganizer()
    {
        var eventId = await CreateEventAsync("Not Yours");
        var stranger = await factory.CreateAuthenticatedClientAsync(
            EventHubWebApplicationFactory.OtherOrganizerEmail);

        var response = await stranger.GetAsync($"/api/events/{eventId}/bookings");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsUpdatedEvent_AfterUpdate()
    {
        var organizer = await factory.CreateAuthenticatedClientAsync(
            EventHubWebApplicationFactory.OrganizerEmail);
        var created = await organizer.PostAsJsonAsync("/api/events", NewEvent("Cache Detail"));
        var location = created.Headers.Location!;

        await factory.CreateClient().GetAsync(location);

        var updated = new
        {
            title = "Cache Detail Updated",
            description = "New Description",
            location = "Kassel",
            startAt = DateTimeOffset.UtcNow.AddHours(5),
            doorsOpenAt = DateTimeOffset.UtcNow.AddHours(4),
            maxParticipants = 100,
            categoryId = EventHubWebApplicationFactory.FirstCategoryId
        };
        (await organizer.PutAsJsonAsync(location, updated)).EnsureSuccessStatusCode();

        var detail = await factory.CreateClient().GetFromJsonAsync<EventDetail>(location.ToString());
        Assert.Equal("Cache Detail Updated", detail!.EventTitle);
    }

    [Fact]
    public async Task GetAll_ContainsNewEvent_AfterCreate()
    {
        var client = factory.CreateClient();
        await client.GetAsync("/api/events");
        await CreateEventAsync("Cache List Entry");

        var events = await client.GetFromJsonAsync<List<EventSummary>>("/api/events");
        Assert.Contains(events!, e => e.Title == "Cache List Entry");
    }

    [Fact]
    public async Task GetById_ReturnsProblemDetails_WhenEventDoesNotExist()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/events/{Guid.NewGuid()}");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(404, problem!.Status);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    [Fact]
    public async Task Upload_StoresObject_AndReturnsMediaPath()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);

        var response = await client.PostAsync("/api/uploads/images", ImageContent());
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);

        var result = await response.Content.ReadFromJsonAsync<UploadResult>();
        Assert.StartsWith("/media/events/", result!.Url);
        Assert.EndsWith(".webp", result.Url);

        var key = result.Url["/media/".Length..];
        var stored = await factory.S3.GetObjectAsync(ImagePath.Bucket, key);

        Assert.Equal("image/webp", stored.Headers.ContentType);
        Assert.True(stored.ContentLength > 0);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileIsNotAnImage()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);

        var content = new MultipartFormDataContent();
        var bytes = new ByteArrayContent([.. "kein bild"u8]);
        bytes.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(bytes, "file", "picture.jpg");

        var response = await client.PostAsync("/api/uploads/images", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenNoFileIsProvided()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);

        var response = await client.PostAsync(
            "/api/uploads/images", new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_ReturnsUnauthorized_WhenNoTokenIsProvided()
    {
        var response = await factory.CreateClient()
            .PostAsync("/api/uploads/images", ImageContent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_ReturnsForbidden_WhenUserIsNotAnOrganizer()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.ParticipantEmail);

        var response = await client.PostAsync("/api/uploads/images", ImageContent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_PersistsImageUrl_AndDetailReturnsIt()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);
        const string url = "/media/events/abc.webp";
        var newEvent = NewEvent("Details with Image", image: url);
        var created = await client.PostAsJsonAsync("/api/events", newEvent);
        created.EnsureSuccessStatusCode();
        var body = await created.Content.ReadAsStringAsync();
        Assert.True(created.IsSuccessStatusCode, body);
        var detailsResponse = await client.GetAsync(created.Headers.Location);
        var detailsBody = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains(url, detailsBody);
        var detail = await client.GetFromJsonAsync<EventDetail>(created.Headers.Location);

        Assert.Equal(url, detail!.EventImageUrl);
    }

    [Fact]
    public async Task GetAll_IncludesImageUrl()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);
        var title = $"List of pictures {Guid.NewGuid():N}";
        const string url = "/media/events/def.webp";

        var created = await client.PostAsJsonAsync(
            "/api/events", NewEvent(title, image: url));

        created.EnsureSuccessStatusCode();

        var all = await client.GetFromJsonAsync<List<EventSummary>>("/api/events");
        var mine = all!.Single(e => e.Title == title);

        Assert.Equal(url, mine.ImageUrl);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenImageUrlIsExternal()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);
        var newEvent = NewEvent("External Image", image: "https://example.com/tracker.gif");

        var response = await client.PostAsJsonAsync("/api/events", newEvent);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, body);
    }

    [Fact]
    public async Task Create_StoresNull_WhenImageUrlIsEmpty()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);
        var newEvent = NewEvent("Empty Image", image: "");

        var created = await client.PostAsJsonAsync("/api/events", newEvent);
        var body = await created.Content.ReadAsStringAsync();
        Assert.True(created.IsSuccessStatusCode, body);

        var detail = await client.GetFromJsonAsync<EventDetail>(created.Headers.Location);

        Assert.Null(detail!.EventImageUrl);
    }

    [Fact]
    public async Task Update_ClearsImageUrl_WhenNullIsSent()
    {
        var client = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);
        const string url = "/media/events/abc.webp";

        var created = await client.PostAsJsonAsync("/api/events", NewEvent("Clear Image", image: url));
        var createBody = await created.Content.ReadAsStringAsync();
        Assert.True(created.IsSuccessStatusCode, createBody);

        var updated = await client.PutAsJsonAsync(created.Headers.Location, NewEvent("Clear Image", image: null));
        var updateBody = await updated.Content.ReadAsStringAsync();
        Assert.True(updated.IsSuccessStatusCode, updateBody);

        var detail = await client.GetFromJsonAsync<EventDetail>(created.Headers.Location);

        Assert.Null(detail!.EventImageUrl);
    }
}