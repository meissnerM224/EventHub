using System.Security.Claims;
using EventHub.Api.Models;

namespace EventHub.Api.Extensions;

public static class ClaimsPrincipalExtension
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException(
                "Token doesn't contains user-Id.");
    }
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal user)
    {
        var name = user.FindFirstValue(ClaimTypes.Name)
                   ?? throw new InvalidOperationException("Token has no name claim.");
        var exp = user.FindFirstValue("exp")
                  ?? throw new InvalidOperationException("Token has no exp claim.");

        return new CurrentUser
        {
            UserId = user.GetUserId(),
            DisplayName = name,
            Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
            ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)),
        };
    }
}