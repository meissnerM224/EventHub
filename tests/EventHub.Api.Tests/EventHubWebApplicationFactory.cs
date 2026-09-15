using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using EventHub.Domain.Authorization;
using EventHub.Domain.Entities;
using EventHub.Domain.Models;
using EventHub.Infrastructure.Entities;
using EventHub.Infrastructure.Persistence;
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
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace EventHub.Api.Tests;

public class EventHubWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestJwtKey = "test-key-only-for-integration-tests-do-not-use-elsewhere";

    public const string OrganizerEmail = "organizer@test.com";
    public const string OtherOrganizerEmail = "other-organizer@test.com";
    public const string ParticipantEmail = "participant@test.com";
    public const string TestPassword = "Test1234!";
    public const int FirstCategoryId = 1;
    public const int SecondCategoryId = 2;
    private const string FirstCategoryName = "Test Category";
    private const string SecondCategoryName = "Second Test Categor";

    public static readonly Guid OrganizerId = Guid.Parse("c1a94f60-3e28-4d7b-8f52-9b0e6a4c2d18");

    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:16-alpine").Build();


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
                ["ConnectionString:Redis"] = _redis.GetConnectionString()
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
        await Task.WhenAll(_container.StartAsync(), _redis.StartAsync());


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

        db.Categories.Add(new Category { Id = FirstCategoryId, Name = FirstCategoryName });
        db.Categories.Add(new Category { Id = SecondCategoryId, Name = SecondCategoryName });


        await db.SaveChangesAsync();
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
                $"Seeding von '{email}' fehlgeschlagen: " +
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
        await base.DisposeAsync();
    }
}