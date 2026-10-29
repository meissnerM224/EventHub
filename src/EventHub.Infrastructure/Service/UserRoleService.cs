using EventHub.Domain.Authorization;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Interfaces;
using EventHub.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;

namespace EventHub.Infrastructure.Service;

public class UserRoleService(UserManager<AppUser> userManager) : IUserRoleService
{
    public async Task GrantOrganizerAsync(Guid userId)
    {
        var user = await FindAsync(userId);

        if (await userManager.IsInRoleAsync(user, RoleName.Organizer)) return;

        EnsureSucceeded(await userManager.AddToRoleAsync(user, RoleName.Organizer));
    }

    public async Task RevokeOrganizerAsync(Guid userId)
    {
        var user = await FindAsync(userId);

        if (!await userManager.IsInRoleAsync(user, RoleName.Organizer)) return;

        EnsureSucceeded(await userManager.RemoveFromRoleAsync(user, RoleName.Organizer));
    }

    private async Task<AppUser> FindAsync(Guid userId) =>
        await userManager.FindByIdAsync(userId.ToString())
        ?? throw new NotFoundException($"User {userId} not found.", userId.ToString());

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}