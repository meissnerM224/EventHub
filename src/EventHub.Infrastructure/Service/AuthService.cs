using EventHub.Domain.Authorization;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using EventHub.Infrastructure.Authentication;
using EventHub.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;

namespace EventHub.Infrastructure.Service;

public class AuthService(
    UserManager<AppUser> userManager,
    IPasswordHasher<AppUser> passwordHasher,
    IJwtTokenGenerator tokenGenerator) : IAuthService
{
    // Hashed once. An unknown email is checked against it so that both cases take the same time
    // and the response time does not reveal which emails are registered.
    private static string? _dummyHash;

    public async Task<AuthResult> RegisterAsync(string email, string password, string displayName)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            throw new AlreadyExistException(ErrorCodes.UserEmailTaken, $"{email} is already registered.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new BusinessRuleException(ErrorCodes.UserRegistrationFailed, string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, RoleName.Participant);
        return BuildResult(user, [RoleName.Participant]);
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            var dummy = new AppUser { DisplayName = "" };
            _dummyHash ??= passwordHasher.HashPassword(dummy, Guid.NewGuid().ToString());
            passwordHasher.VerifyHashedPassword(dummy, _dummyHash, password);
            throw InvalidCredentials();
        }

        // Checked before the password: while locked, even the right password gives no hint.
        if (await userManager.IsLockedOutAsync(user))
            throw new UnauthorizedException(ErrorCodes.AuthLockedOut, "Too many failed attempts, the account is locked for a while.");

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            throw InvalidCredentials();
        }

        await userManager.ResetAccessFailedCountAsync(user);
        var roles = await userManager.GetRolesAsync(user);
        return BuildResult(user, roles);
    }

    public async Task LogoutAsync(Guid userId)
    {
        if (await userManager.FindByIdAsync(userId.ToString()) is { } user)
            await userManager.UpdateSecurityStampAsync(user);
    }

    private static UnauthorizedException InvalidCredentials() =>
        new(ErrorCodes.AuthInvalidCredentials, "Login failed, E-Mail or Password are wrong.");

    private AuthResult BuildResult(AppUser user, IList<string> roles)
    {
        var token = tokenGenerator.Create(user, roles);
        return new AuthResult
        {
            AccessToken = token.Value,
            ExpiresAt = token.ExpiresAt,
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Roles = roles
        };
    }

}