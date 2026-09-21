using EventHub.Api.Extensions;
using EventHub.Api.Models;
using EventHub.Domain.Authorization;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(IEventsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<EventSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<EventSummary>>> GetAll([FromQuery] EventFilter filter)
    {
        return Ok(await service.GetAllEventsAsync(filter));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<EventDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetail>> GetById(Guid id)
    {
        var result = await service.GetEventByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = RoleName.Organizer)]
    [ProducesResponseType<EventSummary>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventSummary>> Create([FromBody] CreateEvent request)
    {
        var result = await service.CreateEventAsync(
            request.Title,
            request.Description,
            request.Location,
            imageUrl: request.Image,
            request.StartAt,
            request.DoorsOpenAt,
            request.MaxParticipants,
            User.GetUserId(),
            request.CategoryId
        );

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleName.Organizer)]
    [ProducesResponseType<EventDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventDetail>> Update(Guid id, [FromBody] UpdateEvent request)
    {
        var result = await service.UpdateEventAsync(
            id,
            request.Title,
            request.Description,
            request.Image,
            request.Location,
            request.StartAt,
            request.DoorsOpenAt,
            request.MaxParticipants,
            request.CategoryId,
            User.GetUserId()
        );

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleName.Organizer)]
    [ProducesResponseType<EventDetail>(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.CancelEventAsync(id, User.GetUserId());
        return NoContent();
    }
}