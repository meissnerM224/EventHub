using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EventHub.Api.Models;
using EventHub.Domain.Models;
using EventHub.Infrastructure.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EventHub.Api.Tests;

/// <summary>
/// Each test replays an attack from the security review and expects it to be stopped.
/// </summary>
[Collection("EventHub")]
public sealed class SecurityTests(EventHubWebApplicationFactory factory)
{
    // ── 1. Password guessing ─────────────────────────────────────────────

    [Fact]
    public async Task Login_LocksTheAccount_AfterFiveWrongPasswords()
    {
        var email = await RegisterAsync();

        for (var i = 0; i < 5; i++)
            await AssertProblem(await LoginAsync(email, "Wrong1234!"),
                HttpStatusCode.Unauthorized, ErrorCodes.AuthInvalidCredentials);

        // Even the right password is refused now – guessing has stopped working.
        await AssertProblem(await LoginAsync(email, EventHubWebApplicationFactory.TestPassword),
            HttpStatusCode.Unauthorized, ErrorCodes.AuthLockedOut);
    }

    [Fact]
    public async Task Login_UnlocksAgain_AfterTheLockoutPeriod()
    {
        var email = await RegisterAsync();
        for (var i = 0; i < 5; i++) await LoginAsync(email, "Wrong1234!");

        // Identity's UserManager reads the system clock, not TimeProvider – so the lockout
        // end is moved into the past instead of moving the clock forward.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByEmailAsync(email);
            Assert.True(await users.IsLockedOutAsync(user!));
            Assert.True((await users.GetLockoutEndDateAsync(user!)) > DateTimeOffset.UtcNow.AddMinutes(14),
                "lockout should last about 15 minutes");
            await users.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddSeconds(-1));
        }

        var response = await LoginAsync(email, EventHubWebApplicationFactory.TestPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_ResetsTheFailureCount_AfterASuccessfulLogin()
    {
        var email = await RegisterAsync();
        for (var i = 0; i < 4; i++) await LoginAsync(email, "Wrong1234!");
        (await LoginAsync(email, EventHubWebApplicationFactory.TestPassword)).EnsureSuccessStatusCode();
        for (var i = 0; i < 4; i++) await LoginAsync(email, "Wrong1234!");

        var response = await LoginAsync(email, EventHubWebApplicationFactory.TestPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Auth_IsRateLimited_PerClient()
    {
        // Lockout protects one account; the rate limit protects against trying many accounts.
        using var app = factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:AuthPermitLimit", "3"));
        var client = app.CreateClient();

        for (var i = 0; i < 3; i++)
            await client.PostAsJsonAsync("/api/auth/login", new { email = $"x{i}@test.de", password = "Wrong1234!" });
        var blocked = await client.PostAsJsonAsync("/api/auth/login", new { email = "y@test.de", password = "Wrong1234!" });

        await AssertProblem(blocked, HttpStatusCode.TooManyRequests, ErrorCodes.AuthTooManyRequests);
        Assert.True(blocked.Headers.RetryAfter is not null, "429 should say when to retry");
    }

    // ── 2. Decompression bomb ────────────────────────────────────────────

    [Fact]
    public async Task Upload_RejectsImagesWithTooManyPixels_BeforeDecoding()
    {
        // Only the header claims 20 000 × 20 000 px (1.6 GB as RGBA) – the file itself is tiny.
        var organizer = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.OrganizerEmail);
        var content = new MultipartFormDataContent();
        var bytes = new ByteArrayContent(PngHeader(20_000, 20_000));
        bytes.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(bytes, "file", "bomb.png");

        var response = await organizer.PostAsync("/api/uploads/images", content);

        await AssertProblem(response, HttpStatusCode.BadRequest, ErrorCodes.FileTooManyPixels);
    }

    // ── 3. Sessions that outlive logout or a role change ─────────────────

    [Fact]
    public async Task Logout_InvalidatesTheToken_NotOnlyTheCookie()
    {
        var email = await RegisterAsync();
        var token = await TokenAsync(email);
        var stolen = BearerClient(token);
        (await stolen.GetAsync("/api/auth/me")).EnsureSuccessStatusCode();

        (await BearerClient(token).PostAsync("/api/auth/logout", null)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Unauthorized, (await stolen.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task RevokedOrganizer_CannotKeepCreatingEvents_WithTheOldToken()
    {
        var email = await RegisterAsync();
        var admin = await factory.CreateAuthenticatedClientAsync(EventHubWebApplicationFactory.AdminEmail);
        var userId = await UserIdAsync(admin, email);
        (await admin.PutAsync($"/api/admin/users/{userId}/roles/organizer", null)).EnsureSuccessStatusCode();
        var organizer = BearerClient(await TokenAsync(email));
        (await organizer.PostAsJsonAsync("/api/events", NewEvent())).EnsureSuccessStatusCode();

        (await admin.DeleteAsync($"/api/admin/users/{userId}/roles/organizer")).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await organizer.PostAsJsonAsync("/api/events", NewEvent())).StatusCode);
    }

    // ── 7. Unbounded input ───────────────────────────────────────────────

    [Fact]
    public async Task EventFilter_RejectsAnOversizedLocation()
    {
        var response = await factory.CreateClient().GetAsync($"/api/events?location={new string('a', 101)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── 8. Information disclosure ────────────────────────────────────────

    [Fact]
    public async Task ApiReference_IsNotServedInProduction()
    {
        using var app = factory.WithWebHostBuilder(b => b.UseEnvironment("Production"));

        var response = await app.CreateClient().GetAsync("/scalar");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private async Task<string> RegisterAsync()
    {
        var email = $"sec-{Guid.NewGuid():N}@test.de";
        var response = await factory.CreateHttpsClient().PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = EventHubWebApplicationFactory.TestPassword,
            displayName = "Security Test",
        });
        response.EnsureSuccessStatusCode();
        return email;
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        factory.CreateHttpsClient(handleCookies: false)
            .PostAsJsonAsync("/api/auth/login", new { email, password });

    // The raw JWT from the Set-Cookie header – what an attacker would steal.
    private async Task<string> TokenAsync(string email)
    {
        var response = await LoginAsync(email, EventHubWebApplicationFactory.TestPassword);
        response.EnsureSuccessStatusCode();
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("eventhub_auth="));
        return cookie["eventhub_auth=".Length..cookie.IndexOf(';')];
    }

    private HttpClient BearerClient(string token)
    {
        var client = factory.CreateHttpsClient(handleCookies: false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<Guid> UserIdAsync(HttpClient admin, string email)
    {
        var users = await admin.GetFromJsonAsync<List<JsonElement>>("/api/admin/users");
        return users!.Single(u => u.GetProperty("email").GetString() == email).GetProperty("userId").GetGuid();
    }

    private static object NewEvent() => new
    {
        title = $"Security {Guid.NewGuid():N}",
        description = "Test description",
        location = "Theater Kassel",
        startAt = DateTimeOffset.UtcNow.AddDays(2),
        doorsOpenAt = DateTimeOffset.UtcNow.AddDays(2).AddHours(-1),
        maxParticipants = 10,
        categoryId = EventHubWebApplicationFactory.FirstCategoryId,
    };

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"Expected {(int)status}, got {(int)response.StatusCode}: {body}");
        using var json = JsonDocument.Parse(body);
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
    }

    // A PNG that is only signature + IHDR + IEND: valid header, no pixel data.
    private static byte[] PngHeader(int width, int height)
    {
        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(0), width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 6; // RGBA
        WriteChunk(png, "IHDR", ihdr);
        // A few bytes of zlib data – the decoder needs to see IDAT to report the size,
        // a real bomb would continue here with megabytes of compressed zeros.
        WriteChunk(png, "IDAT", [0x78, 0x9C, 0x63, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01]);
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream png, string type, byte[] data)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, data.Length);
        png.Write(buffer);
        var typeAndData = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        png.Write(typeAndData);
        BinaryPrimitives.WriteUInt32BigEndian(buffer, Crc32(typeAndData));
        png.Write(buffer);
    }

    private static uint Crc32(byte[] bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }

        return ~crc;
    }
}
