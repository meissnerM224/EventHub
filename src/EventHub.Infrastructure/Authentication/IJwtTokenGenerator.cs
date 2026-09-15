using EventHub.Infrastructure.Entities;

namespace EventHub.Infrastructure.Authentication;

public sealed record JwtToken(string Value, DateTimeOffset ExpiresAt);

public interface IJwtTokenGenerator
{
    JwtToken Create(AppUser user, IEnumerable<string> roles);
}