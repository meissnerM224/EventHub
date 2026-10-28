using DotNet.Testcontainers.Containers;
using System.Net.Http.Json;
using Amazon.S3;
using DotNet.Testcontainers.Builders;
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
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace EventHub.Api.Tests;

[UsedImplicitly]
public sealed class EventHubWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestJwtKey = "test-key-only-for-integration-tests-do-not-use-elsewhere";
    public const string TestPassword = "Test1234!";
    private const string GarageImage = "dxflrs/garage:ddb90621c1e7e9d98cf0b4088802f31cac57587b";
    private const string TestAccessKey = "GKtest00000000000000test";
    private const string TestSecretKey = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string RpcSecret = "0000000000000000000000000000000000000000000000000000000000000001";
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

    private readonly IContainer _garage = new ContainerBuilder(GarageImage)
        .WithEnvironment("GARAGE_RPC_SECRET", RpcSecret)
        .WithResourceMapping(
            new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "garage.toml")),
            "/etc/")
        .WithPortBinding(3900, true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilMessageIsLogged("S3 API server listening"))
        .Build();

    public HttpClient CreateHttpsClient(bool handleCookies = true) =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies =  handleCookies,
        });
    
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
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync(), _garage.StartAsync());
        var status = await Garage("status");
        var nodeId = status.Split('\n')
            .First(l => l.Contains("NO ROLE ASSIGNED"))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

        await Garage("layout", "assign", "-z", "dc1", "-c", "1G", nodeId);
        await Garage("layout", "apply", "--version", "1");
        await Garage("bucket", "create", ImagePath.Bucket);
        await Garage("key", "import", "--yes", TestAccessKey, TestSecretKey, "-n", "test");
        await Garage("bucket", "allow", "--read", "--write", ImagePath.Bucket, "--key", TestAccessKey);

        _environment = new Dictionary<string, string>
        {
            ["ConnectionStrings__Default"] = _postgres.GetConnectionString(),
            ["ConnectionStrings__Redis"] = _redis.GetConnectionString(),
            ["Jwt__Key"] = TestJwtKey,
            ["Jwt__Issuer"] = "EventHub.Tests",
            ["Jwt__Audience"] = "EventHub.Tests",
            ["Storage__Endpoint"] = $"http://{_garage.Hostname}:{_garage.GetMappedPublicPort(3900)}",
            ["Storage__AccessKey"] = TestAccessKey,
            ["Storage__SecretKey"] = TestSecretKey,
            ["Outbox__PollIntervalMs"] = "200",
            ["Storage__Bucket"] = ImagePath.Bucket,
        };
        var endpoint = $"http://{_garage.Hostname}:{_garage.GetMappedPublicPort(3900)}";
        foreach (var (key, value) in _environment) Environment.SetEnvironmentVariable(key, value);
        S3 = new AmazonS3Client(TestAccessKey, TestSecretKey, new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = "garage"
        });
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventHubDbContext>();
        await db.Database.MigrateAsync();

        var manager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        await CreateUserAsync(manager, OrganizerId, OrganizerEmail, "Test Organizer", RoleName.Organizer);
        await CreateUserAsync(manager, Guid.NewGuid(), OtherOrganizerEmail, "O", RoleName.Organizer);
        await CreateUserAsync(manager, Guid.NewGuid(), ParticipantEmail, "Test", RoleName.Participant);
    }

    async Task<string> Garage(params string[] args)
    {
        var result = await _garage.ExecAsync(["/garage", .. args]);

        if (result.ExitCode != 0)
            throw new InvalidOperationException($"garage {string.Join(' ', args)}: {result.Stderr}");
        return result.Stdout;
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
        var client = CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = TestPassword });
        response.EnsureSuccessStatusCode();

        return client;
    }

    public new async Task DisposeAsync()
    {
        foreach (var key in _environment.Keys) Environment.SetEnvironmentVariable(key, null);
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await _garage.DisposeAsync();
        await base.DisposeAsync();
    }
}