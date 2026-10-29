using System.Net;
using System.Net.Http.Json;
using EventHub.Api.Models;
using EventHub.Domain.Authorization;

namespace EventHub.Api.Tests;

[Collection("EventHub")]
public class AdminUserRolesTests(EventHubWebApplicationFactory factory)
{
    private const string MeUrl = "/api/auth/me";

    private static string OrganizerRoleUrl(Guid userId) =>
        $"/api/admin/users/{userId}/roles/organizer";


    [Fact]
    public async Task Register_IgnoresRoleInBody_AndAssignsParticipant()
    {
        var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"sneaky-{Guid.NewGuid():N}@test.de",
            password = EventHubWebApplicationFactory.TestPassword,
            displayName = "Sneaky",
            role = RoleName.Organizer,
        });

        await AssertStatus(HttpStatusCode.OK, response);
        var me = await response.Content.ReadFromJsonAsync<CurrentUser>();
        Assert.Equal([RoleName.Participant], me!.Roles);
    }

    // ── Access ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Grant_ReturnsUnauthorized_WithoutCookie()
    {
        var (_, user) = await RegisterFreshUserAsync();
        var client = factory.CreateHttpsClient();

        var response = await client.PutAsync(OrganizerRoleUrl(user.UserId), null);

        await AssertStatus(HttpStatusCode.Unauthorized, response);
    }

    [Fact]
    public async Task Grant_ReturnsForbidden_WhenCallerIsNotAdmin()
    {
        var (_, user) = await RegisterFreshUserAsync();
        // An organizer must not be able to create other organizers.
        var client = await factory.CreateAuthenticatedClientAsync(
            EventHubWebApplicationFactory.OrganizerEmail);

        var response = await client.PutAsync(OrganizerRoleUrl(user.UserId), null);

        await AssertStatus(HttpStatusCode.Forbidden, response);
    }

    [Fact]
    public async Task Grant_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var admin = await AdminClientAsync();

        var response = await admin.PutAsync(OrganizerRoleUrl(Guid.NewGuid()), null);

        await AssertStatus(HttpStatusCode.NotFound, response);
    }

    // ── Grant ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Grant_TakesEffect_AtNextLogin()
    {
        var (userClient, user) = await RegisterFreshUserAsync();
        var admin = await AdminClientAsync();

        await AssertStatus(HttpStatusCode.NoContent,
            await admin.PutAsync(OrganizerRoleUrl(user.UserId), null));

        // The existing token was issued before the change and still says
        // Participant. This pins the trade-off of stateless auth (Session 3).
        var before = await MeAsync(userClient);
        Assert.DoesNotContain(RoleName.Organizer, before.Roles);

        var reloggedClient = await factory.CreateAuthenticatedClientAsync(user.Email);
        var after = await MeAsync(reloggedClient);
        Assert.Contains(RoleName.Organizer, after.Roles);
        Assert.Contains(RoleName.Participant, after.Roles);
    }

    [Fact]
    public async Task Grant_IsIdempotent()
    {
        var (_, user) = await RegisterFreshUserAsync();
        var admin = await AdminClientAsync();

        await AssertStatus(HttpStatusCode.NoContent,
            await admin.PutAsync(OrganizerRoleUrl(user.UserId), null));
        await AssertStatus(HttpStatusCode.NoContent,
            await admin.PutAsync(OrganizerRoleUrl(user.UserId), null));
    }

    // ── Revoke ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Revoke_TakesEffect_AtNextLogin()
    {
        var (_, user) = await RegisterFreshUserAsync();
        var admin = await AdminClientAsync();
        await AssertStatus(HttpStatusCode.NoContent,
            await admin.PutAsync(OrganizerRoleUrl(user.UserId), null));

        await AssertStatus(HttpStatusCode.NoContent,
            await admin.DeleteAsync(OrganizerRoleUrl(user.UserId)));

        var relogged = await factory.CreateAuthenticatedClientAsync(user.Email);
        var me = await MeAsync(relogged);
        Assert.DoesNotContain(RoleName.Organizer, me.Roles);
        Assert.Contains(RoleName.Participant, me.Roles);
    }

    [Fact]
    public async Task Revoke_IsIdempotent_WhenUserIsNotOrganizer()
    {
        var (_, user) = await RegisterFreshUserAsync();
        var admin = await AdminClientAsync();

        var response = await admin.DeleteAsync(OrganizerRoleUrl(user.UserId));

        await AssertStatus(HttpStatusCode.NoContent, response);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private Task<HttpClient> AdminClientAsync() =>
        factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.AdminEmail);

    // A fresh user per test: role changes on a shared seeded user would
    // couple tests to each other (Session 2, static readonly ValidEvent).
    private async Task<(HttpClient Client, FreshUser User)> RegisterFreshUserAsync()
    {
        var client = factory.CreateHttpsClient();
        var email = $"user-{Guid.NewGuid():N}@test.de";

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = EventHubWebApplicationFactory.TestPassword,
            displayName = "Fresh User",
        });
        await AssertStatus(HttpStatusCode.OK, response);

        var current = await response.Content.ReadFromJsonAsync<CurrentUser>();
        return (client, new FreshUser(current!.UserId, email));
    }

    private static async Task<CurrentUser> MeAsync(HttpClient client)
    {
        var response = await client.GetAsync(MeUrl);
        await AssertStatus(HttpStatusCode.OK, response);
        return (await response.Content.ReadFromJsonAsync<CurrentUser>())!;
    }

    private static async Task AssertStatus(HttpStatusCode expected, HttpResponseMessage response) =>
        Assert.True(
            response.StatusCode == expected,
            $"Expected {(int)expected}, got {(int)response.StatusCode}: " +
            await response.Content.ReadAsStringAsync());

    private sealed record FreshUser(Guid UserId, string Email);
}