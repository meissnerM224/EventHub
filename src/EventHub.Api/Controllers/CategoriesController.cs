using EventHub.Api.Models;
using EventHub.Domain.Authorization;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(IEventsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategorySummary>), 200)]
    public async Task<ActionResult<IEnumerable<CategorySummary>>> GetAll()
    {
        return Ok(await service.GetCategoriesAsync());
    }

    [HttpPost]
    [Authorize(Roles = RoleName.Admin)]
    [ProducesResponseType<CategorySummary>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategorySummary>> Create(CreateCategory body)
    {
        var created = await service.CreateCategoryAsync(body.Name, body.Description);
        return StatusCode(StatusCodes.Status201Created, created);
    }
}