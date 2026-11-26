namespace EventHub.Domain.Models;

public class AdminUserSummary
{
    public required Guid UserId { get; init; }
    public required string DisplayName { get; init; }
    public required string Email { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }

}