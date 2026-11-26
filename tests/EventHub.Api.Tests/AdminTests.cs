using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventHub.Api.Tests;

[Collection("EventHub")]
public class AdminTests(EventHubWebApplicationFactory factory)
{
    // Own shapes instead of the production models (Session 7 §12).
    private sealed record CategoryResponse(int Id, string Name);
    private sealed record AdminUserResponse(string Email, List<string> Roles);

    private Task<HttpClient> AdminAsync() =>
        factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.AdminEmail);

    private Task<HttpClient> ParticipantAsync() =>
        factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.ParticipantEmail);

    [Fact]
    public async Task CreateCategory_AsAdmin_IsListedImmediately()
    {
        var admin = await AdminAsync();
        var name = $"Lesung {Guid.NewGuid():N}";
        // Warm the cache first – the POST must remove it, or the GET below is stale.
        (await admin.GetAsync("/api/categories")).EnsureSuccessStatusCode();

        var response = await admin.PostAsJsonAsync("/api/categories", new { name, description = "" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var all = await admin.GetFromJsonAsync<List<CategoryResponse>>("/api/categories");
        Assert.Contains(all!, c => c.Name == name);
    }

    [Fact]
    public async Task CreateCategory_WithExistingName_IgnoringCase_Returns409WithCode()
    {
        var admin = await AdminAsync();

        var response = await admin.PostAsJsonAsync("/api/categories",
            new { name = "konzert", description = "" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("category.already_exists", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateCategory_AsParticipant_ReturnsForbidden()
    {
        var participant = await ParticipantAsync();

        var response = await participant.PostAsJsonAsync("/api/categories",
            new { name = "Nicht erlaubt", description = "" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AsAdmin_ReturnsUsersWithRoles()
    {
        var admin = await AdminAsync();

        var users = await admin.GetFromJsonAsync<List<AdminUserResponse>>("/api/admin/users");

        Assert.Contains(users!, u =>
            u.Email == EventHubWebApplicationFactory.AdminEmail && u.Roles.Contains("Administrator"));
        Assert.Contains(users!, u =>
            u.Email == EventHubWebApplicationFactory.OrganizerEmail && u.Roles.Contains("Organizer"));
    }

    [Fact]
    public async Task GetUsers_AsParticipant_ReturnsForbidden()
    {
        var participant = await ParticipantAsync();

        var response = await participant.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}