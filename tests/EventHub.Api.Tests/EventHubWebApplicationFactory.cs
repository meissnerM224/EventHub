using EventHub.Domain.Entities;
using EventHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace EventHub.Api.Tests;

public class EventHubWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
        });
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventHubDbContext>();
        await db.Database.MigrateAsync();

        db.Users.Add(new User
        {
            Id = Guid.Parse("c1a94f60-3e28-4d7b-8f52-9b0e6a4c2d18"),
            Name = "Test Organizer",
            Email = "organizer@test.com"
        });
        db.Categories.Add(new Category { Id = 1, Name = "Test Category" });
        await db.SaveChangesAsync();
    }

    public new async Task DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }
}