using EventHub.Domain.Authorization;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = RoleName.Admin)]
public class AdminUserRolesController(IUserRoleService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminUserSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AdminUserSummary>>> GetAll()
    {
        return Ok(await service.GetUsersAsync());
    }

    [HttpPut("{userId:guid}/roles/organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Grant(Guid userId)
    {
        await service.GrantOrganizerAsync(userId);
        return NoContent();
    }

    [HttpDelete("{userId:guid}/roles/organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(Guid userId)
    {
        await service.RevokeOrganizerAsync(userId);
        return NoContent();
    }
}