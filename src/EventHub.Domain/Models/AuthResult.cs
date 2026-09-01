namespace EventHub.Domain.Models;

public class AuthResult
{
    public required string AccessToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required Guid UserId { get; init; }
    public required string DisplayName { get; init; }
    public required IList<string> Roles { get; init; }
}