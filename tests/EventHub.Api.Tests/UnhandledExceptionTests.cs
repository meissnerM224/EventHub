using System.Net;
using System.Reflection;
using System.Text.Json;
using EventHub.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventHub.Api.Tests;

/// <summary>
/// An exception that is not a DomainException ends in UnhandledExceptionHandler.
/// Outside Development the response must not reveal internals (message, stack trace),
/// but must carry a traceId so the error can be found in the logs.
/// </summary>
[Collection("EventHub")]
public sealed class UnhandledExceptionTests(EventHubWebApplicationFactory factory)
{
    private const string SecretMessage = "connection string Password=hunter2 leaked";

    [Fact]
    public async Task Production_Returns500ProblemDetails_WithoutInternals()
    {
        using var app = WithFailingCache("Production");

        var response = await app.CreateClient().GetAsync("/api/categories");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        Assert.Equal(500, root.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred", root.GetProperty("title").GetString());
        Assert.Equal("See the traceId in this response when contacting support.",
            root.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        // No domain code: the frontend shows its generic server error for this.
        Assert.False(root.TryGetProperty("code", out _));

        Assert.DoesNotContain(SecretMessage, body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
        Assert.DoesNotContain(" at ", body); // stack frame
    }

    [Fact]
    public async Task Development_IncludesTheExceptionForDebugging()
    {
        using var app = WithFailingCache("Development");

        var response = await app.CreateClient().GetAsync("/api/categories");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains(SecretMessage, body);
    }

    // Same containers (configured through environment variables), different host:
    // every cache call throws an exception the domain handler does not know.
    private WebApplicationFactory<Program> WithFailingCache(string environment) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICacheService>();
                services.AddScoped(_ => ThrowingCache.Create());
            });
        });

    public class ThrowingCache : DispatchProxy
    {
        public static ICacheService Create() => Create<ICacheService, ThrowingCache>();

        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            throw new InvalidOperationException(SecretMessage);
    }
}
