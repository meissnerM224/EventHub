using System.Net;
using System.Net.Http.Json;
using System.Text;
using EventHub.Domain.Models;

namespace EventHub.Api.Tests;

public sealed class EndpointTests : IClassFixture<EventHubWebApplicationFactory>
{
    private readonly EventHubWebApplicationFactory _factory;

    public EndpointTests(EventHubWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static object NewEvent(string title) => new
    {
        title,
        description = "Testbeschreibung",
        location = "Staatstheater Kassel",
        startAt = DateTimeOffset.UtcNow.AddHours(2),
        doorsOpenAt = DateTimeOffset.UtcNow.AddHours(1),
        maxParticipants = 1000,
        organizerId = "c1a94f60-3e28-4d7b-8f52-9b0e6a4c2d18",
        categoryId = 1
    };

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenEventExists()
    {
        var client = _factory.CreateClient();

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
        var response = await _factory.CreateClient().GetAsync($"/api/events/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenIdIsNotAGuid()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events/wadnlwadawdaq231");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenBodyIsEmpty()
    {
        var response = await _factory.CreateClient().PostAsync(
            "/api/events",
            new StringContent("", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenEventAlreadyExists()
    {
        var client = _factory.CreateClient();
        var payload = NewEvent("Konflikt Test");
        var test = await client.PostAsJsonAsync("/api/events", payload);
        var response = await client.PostAsJsonAsync("/api/events", payload);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenEventExists()
    {
        var client = _factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/events", NewEvent("Update Happy Path"));
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var location = postResponse.Headers.Location;
        Assert.NotNull(location);

        var updated = new
        {
            title = "Geänderter Titel",
            description = "Neue Beschreibung",
            location = "Kulturzentrum Kassel",
            startAt = DateTimeOffset.UtcNow.AddHours(5),
            doorsOpenAt = DateTimeOffset.UtcNow.AddHours(4),
            maxParticipants = 500,
            categoryId = 1
        };

        var putResponse = await client.PutAsJsonAsync(location, updated);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var getResponse = await client.GetAsync(location);
        var detail = await getResponse.Content.ReadFromJsonAsync<EventDetail>();
        Assert.Equal("Geänderter Titel", detail!.EventTitle);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenEventDoesNotExist()
    {
        var updated = new
        {
            title = "Egal",
            description = "Egal",
            location = "Egal",
            startAt = DateTimeOffset.UtcNow.AddHours(5),
            doorsOpenAt = DateTimeOffset.UtcNow.AddHours(4),
            maxParticipants = 100,
            categoryId = 1
        };

        var response = await _factory.CreateClient()
            .PutAsJsonAsync($"/api/events/{Guid.NewGuid()}", updated);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenStartAtIsInThePast()
    {
        var client = _factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/events", NewEvent("Update Vergangenheit"));
        var location = postResponse.Headers.Location;

        var updated = new
        {
            title = "Titel",
            description = "Beschreibung",
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
        var client = _factory.CreateClient();
        var postResponse = await client.PostAsJsonAsync("/api/events", NewEvent("Delete Test"));
        var location = postResponse.Headers.Location;

        var deleteResponse = await client.DeleteAsync(location);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenEventDoesNotExist()
    {
        var response = await _factory.CreateClient().DeleteAsync($"/api/events/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_IsIdempotent()
    {
        var client = _factory.CreateClient();
        var postResponse = await client.PostAsJsonAsync("/api/events", NewEvent("Delete Idempotent"));
        var location = postResponse.Headers.Location;

        await client.DeleteAsync(location);
        var second = await client.DeleteAsync(location);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }
}