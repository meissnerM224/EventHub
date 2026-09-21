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
    IJwtTokenGenerator tokenGenerator) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(string email, string password, string displayName, string role)
    {
        if (role is not RoleName.Organizer && role is not RoleName.Participant)
        {
            throw new BusinessRuleException($"Unbekannte Rolle '{role}'.");
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            throw new AlreadyExistException($"{email} is already registered.");
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
            throw new BusinessRuleException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, role);
        return BuildResult(user, [role]);
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            throw new UnauthorizedException("Login failed, E-Mail or Password are wrong.");
        }

        var roles = await userManager.GetRolesAsync(user);
        return BuildResult(user, roles);
    }

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