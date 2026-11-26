using EventHub.Domain.Entities;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
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
}