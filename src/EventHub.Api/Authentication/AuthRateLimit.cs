using System.Globalization;
using System.Threading.RateLimiting;
using EventHub.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventHub.Api.Authentication;

/// <summary>
/// Login and register per client IP. The account lockout protects a single account;
/// this stops one client from trying many accounts or creating them in bulk.
/// The client IP is the real one behind Caddy thanks to UseForwardedHeaders.
/// </summary>
public static class AuthRateLimit
{
    public const string Policy = "auth";
    public const string PermitLimitKey = "RateLimiting:AuthPermitLimit";

    public static IServiceCollection AddAuthRateLimit(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(Policy, context =>
            {
                // Read per request, so tests and deployments can change it via configuration.
                var limit = context.RequestServices.GetRequiredService<IConfiguration>()
                    .GetValue(PermitLimitKey, 10);
                return RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    });
            });

            options.OnRejected = async (context, ct) =>
            {
                var http = context.HttpContext;
                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    http.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

                await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(
                    new ProblemDetailsContext
                    {
                        HttpContext = http,
                        ProblemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = "Too many requests",
                            Detail = "Too many attempts, please try again later.",
                            Extensions = { ["code"] = ErrorCodes.AuthTooManyRequests },
                        },
                    });
            };
        });
}
