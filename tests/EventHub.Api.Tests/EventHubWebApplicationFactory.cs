using System.Net.Http.Headers;
using System.Net.Http.Json;
using Amazon.S3;
using EventHub.Api.Tests.Fakes;
using EventHub.Domain.Authorization;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using EventHub.Domain.Storage;
using EventHub.Infrastructure.Entities;
using EventHub.Infrastructure.Persistence;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace EventHub.Api.Tests;

[UsedImplicitly]
public sealed class EventHubWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestJwtKey = "test-key-only-for-integration-tests-do-not-use-elsewhere";
    private const string MiniIoUser = "testuser";
    public const string TestPassword = "Test1234!";

    public const string OrganizerEmail = "organizer@test.com";
    public const string OtherOrganizerEmail = "other-organizer@test.com";
    public const string ParticipantEmail = "participant@test.com";
    public const int FirstCategoryId = 1;
    public const int SecondCategoryId = 2;
    public const string FirstCategoryName = "Konzert";
    private static readonly Guid OrganizerId = Guid.Parse("c1a94f60-3e28-4d7b-8f52-9b0e6a4c2d18");

    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:17-alpine").Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    private readonly MinioContainer _minio = new MinioBuilder("quay.io/minio/minio:RELEASE.2025-09-07T16-13-09Z")
        .WithUsername(MiniIoUser)
        .WithPassword(TestPassword)
        .Build();

    private Dictionary<string, string> _environment = [];
    public IAmazonS3 S3 { get; private set; } = null!;

    public RecordingNotificationSender Notifications =>
        Services.GetRequiredService<RecordingNotificationSender>();


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<INotificationSender>();
            services.AddSingleton<RecordingNotificationSender>();
            services.AddScoped<INotificationSender>(sp => sp.GetRequiredService<RecordingNotificationSender>());
        });
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync(), _minio.StartAsync());
        _environment = new Dictionary<string, string>
        {
            ["ConnectionStrings__Default"] = _postgres.GetConnectionString(),
            ["ConnectionStrings__Redis"] = _redis.GetConnectionString(),
            ["Jwt__Key"] = TestJwtKey,
            ["Jwt__Issuer"] = "EventHub.Tests",
            ["Jwt__Audience"] = "EventHub.Tests",
            ["Storage__Endpoint"] = _minio.GetConnectionString(),
            ["Storage__AccessKey"] = MiniIoUser,
            ["Storage__SecretKey"] = TestPassword,
            ["Outbox__PollIntervalMs"] = "200",
            ["Storage__Bucket"] = ImagePath.Bucket,
        };
        foreach (var (key, value) in _environment) Environment.SetEnvironmentVariable(key, value);
        S3 = new AmazonS3Client(MiniIoUser, TestPassword, new AmazonS3Config
        {
            ServiceURL = _minio.GetConnectionString(),
            ForcePathStyle = true
        });
        await S3.PutBucketAsync(ImagePath.Bucket);
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventHubDbContext>();
        await db.Database.MigrateAsync();

        var manager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        await CreateUserAsync(manager, OrganizerId, OrganizerEmail, "Test Organizer", RoleName.Organizer);
        await CreateUserAsync(manager, Guid.NewGuid(), OtherOrganizerEmail, "O", RoleName.Organizer);
        await CreateUserAsync(manager, Guid.NewGuid(), ParticipantEmail, "Test", RoleName.Participant);
    }


    private static async Task CreateUserAsync(
        UserManager<AppUser> userManager,
        Guid id, string email, string displayName, string role)
    {
        var user = new AppUser
        {
            Id = id,
            UserName = email,
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, TestPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Seeding from '{email}' failed: " +
                string.Join(" ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, role);
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = TestPassword });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResult>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return client;
    }

    public new async Task DisposeAsync()
    {
        foreach (var key in _environment.Keys) Environment.SetEnvironmentVariable(key, null);
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await _minio.DisposeAsync();
        await base.DisposeAsync();
    }
}