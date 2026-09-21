using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Amazon.S3;
using EventHub.Api.Tests.Fakes;
using EventHub.Domain.Authorization;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using EventHub.Infrastructure.Entities;
using EventHub.Infrastructure.Persistence;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace EventHub.Api.Tests;

[UsedImplicitly]
public sealed class EventHubWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestJwtKey = "test-key-only-for-integration-tests-do-not-use-elsewhere";
    public const string OrganizerEmail = "organizer@test.com";
    public const string OtherOrganizerEmail = "other-organizer@test.com";
    public const string ParticipantEmail = "participant@test.com";
    public const string TestPassword = "Test1234!";
    private const string MioIoUser = "testuser";
    public const int FirstCategoryId = 1;
    public const int SecondCategoryId = 2;
    public const string FirstCategoryName = "Konzert";
    private static readonly Guid OrganizerId = Guid.Parse("c1a94f60-3e28-4d7b-8f52-9b0e6a4c2d18");
    public const string Bucket = "media";

    public RecordingNotificationSender Notifications =>
        Services.GetRequiredService<RecordingNotificationSender>();

    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17-alpine").Build();

    private readonly MinioContainer _minio = new MinioBuilder("quay.io/minio/minio:RELEASE.2025-09-07T16-13-09Z")
        .WithUsername(MioIoUser)
        .WithPassword(TestPassword)
        .Build();

    public IAmazonS3 S3 { get; private set; } = null!;

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = "EventHub.Tests",
                ["Jwt:Audience"] = "EventHub.Tests",
                ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
                ["Outbox:PollIntervalMs"] = "200",
                ["Storage:Endpoint"] = _minio.GetConnectionString(),
                ["Storage:AccessKey"] = MioIoUser,
                ["Storage:SecretKey"] = TestPassword,
                ["Storage:Bucket"] = Bucket,
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<EventHubDbContext>>();

            var toRemove = services
                .Where(d => d.ServiceType.IsGenericType &&
                            d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration") &&
                            d.ServiceType.GenericTypeArguments.Length == 1 &&
                            d.ServiceType.GenericTypeArguments[0] == typeof(EventHubDbContext))
                .ToList();
            toRemove.ForEach(d => services.Remove(d));
            services.RemoveAll<INotificationSender>();
            services.AddSingleton<RecordingNotificationSender>();
            services.AddScoped<INotificationSender>(sp => sp.GetRequiredService<RecordingNotificationSender>());
            services.AddSingleton<RecordingNotificationSender>();
            services.AddScoped<INotificationSender>(sp => sp.GetRequiredService<RecordingNotificationSender>());
            services.AddDbContext<EventHubDbContext>(options =>
                options.UseNpgsql(_container.GetConnectionString()));
            // Program.cs captures jwt from builder.Configuration before test overrides apply,
            // so we must also override the validation parameters here.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
                options.TokenValidationParameters.ValidIssuer = "EventHub.Tests";
                options.TokenValidationParameters.ValidAudience = "EventHub.Tests";
            });
        });
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_container.StartAsync(), _redis.StartAsync(), _minio.StartAsync());


        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventHubDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();


        await CreateUserAsync(userManager, OrganizerId, OrganizerEmail,
            "Test Organizer", RoleName.Organizer);
        await CreateUserAsync(userManager, Guid.NewGuid(), OtherOrganizerEmail,
            "Other Organizer", RoleName.Organizer);
        await CreateUserAsync(userManager, Guid.NewGuid(), ParticipantEmail,
            "Test Participant", RoleName.Participant);

        await db.SaveChangesAsync();

        S3 = new AmazonS3Client(MioIoUser, TestPassword, new AmazonS3Config
        {
            ServiceURL = _minio.GetConnectionString(),
            ForcePathStyle = true
        });
        await S3.PutBucketAsync(Bucket);
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
        await _container.DisposeAsync();
        await _redis.DisposeAsync();
        await _minio.DisposeAsync();
        await base.DisposeAsync();
    }
}