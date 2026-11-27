using EventHub.Domain.Models;

namespace EventHub.Domain.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string displayName);
    Task<AuthResult> LoginAsync(string email, string password);

    // Ends every session of the user: tokens issued before carry an outdated security stamp.
    Task LogoutAsync(Guid userId);
}