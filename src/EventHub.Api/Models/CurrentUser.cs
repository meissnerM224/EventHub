namespace EventHub.Api.Models;

public sealed class CurrentUser
{
    public required Guid UserId { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}