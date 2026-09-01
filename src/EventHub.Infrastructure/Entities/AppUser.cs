using Microsoft.AspNetCore.Identity;
namespace EventHub.Infrastructure.Entities;

public class AppUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}