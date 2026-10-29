using System.Net;
using System.Net.Http.Json;
using EventHub.Api.Authentication;
using EventHub.Api.Models;
using EventHub.Domain.Authorization;

namespace EventHub.Api.Tests;

[Collection("EventHub")]
public class AuthEndpointTests(EventHubWebApplicationFactory factory)
{
    private const string LoginUrl = "/api/auth/login";
    private const string LogoutUrl = "/api/auth/logout";
    private const string MeUrl = "/api/auth/me";

    private static object Credentials(string email) => new
    {
        Email = email,
        Password = EventHubWebApplicationFactory.TestPassword,
    };

    // ── Login ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_SetsHttpOnlyAuthCookie()
    {
        // HandleCookies = false: the client must not consume the header,
        // otherwise there is nothing left to inspect.
        var client = factory.CreateHttpsClient(handleCookies: false);

        var response = await client.PostAsJsonAsync(
            LoginUrl, Credentials(EventHubWebApplicationFactory.OrganizerEmail));

        await AssertStatus(HttpStatusCode.OK, response);

        var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith($"{AuthCookie.Name}=", setCookie);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_ResponseContainsNoToken()
    {
        var client = factory.CreateHttpsClient(handleCookies: false);

        var response = await client.PostAsJsonAsync(
            LoginUrl, Credentials(EventHubWebApplicationFactory.OrganizerEmail));

        await AssertStatus(HttpStatusCode.OK, response);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("accessToken", body, StringComparison.OrdinalIgnoreCase);
        // Every JWT starts with "eyJ" (base64 of '{"'). Catches the token
        // under any property name, not just "accessToken".
        Assert.DoesNotContain("eyJ", body);
    }

    // ── Me ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Me_ReturnsCurrentUser_WhenCookieIsPresent()
    {
        var client = await factory.CreateAuthenticatedClientAsync(
            EventHubWebApplicationFactory.OrganizerEmail);

        var response = await client.GetAsync(MeUrl);

        await AssertStatus(HttpStatusCode.OK, response);

        var me = await response.Content.ReadFromJsonAsync<CurrentUser>();
        Assert.NotNull(me);
        Assert.Equal("Test Organizer", me.DisplayName);
        Assert.Contains(RoleName.Organizer, me.Roles);
        Assert.True(me.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Me_ReturnsUnauthorized_WithoutCookie()
    {
        var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(MeUrl);

        await AssertStatus(HttpStatusCode.Unauthorized, response);
    }

    // ── Logout ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_RemovesCookie()
    {
        var client = await factory.CreateAuthenticatedClientAsync(
            EventHubWebApplicationFactory.OrganizerEmail);

        // Precondition: logged in. Without this the 401 below would prove nothing.
        await AssertStatus(HttpStatusCode.OK, await client.GetAsync(MeUrl));

        var logout = await client.PostAsync(LogoutUrl, content: null);
        await AssertStatus(HttpStatusCode.NoContent, logout);

        var after = await client.GetAsync(MeUrl);
        await AssertStatus(HttpStatusCode.Unauthorized, after);
    }

    [Fact]
    public async Task Logout_IsIdempotent_WhenNotLoggedIn()
    {
        var client = factory.CreateHttpsClient();

        var response = await client.PostAsync(LogoutUrl, content: null);

        await AssertStatus(HttpStatusCode.NoContent, response);
    }

    // ── Helper ───────────────────────────────────────────────────────────

    private static async Task AssertStatus(HttpStatusCode expected, HttpResponseMessage response) =>
        Assert.True(
            response.StatusCode == expected,
            $"Expected {(int)expected}, got {(int)response.StatusCode}: " +
            await response.Content.ReadAsStringAsync());
}