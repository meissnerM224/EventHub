using EventHub.Domain.Authorization;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using EventHub.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using EventHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Service;

public class UserRoleService(UserManager<AppUser> userManager, EventHubDbContext db) : IUserRoleService
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

    public async Task<IReadOnlyList<AdminUserSummary>> GetUsersAsync() =>
        await db.Users
            .AsNoTracking()
            .OrderBy(u => u.DisplayName)
            .Select(u => new AdminUserSummary
            {
                UserId = u.Id,
                DisplayName = u.DisplayName,
                Email = u.Email ?? string.Empty,
                Roles = db.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name!)
                    .OrderBy(name => name)
                    .ToList()
            })
            .ToListAsync();

    private async Task<AppUser> FindAsync(Guid userId) =>
        await userManager.FindByIdAsync(userId.ToString())
        ?? throw new NotFoundException(ErrorCodes.UserNotFound, "User", userId);

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}