using EventHub.Api.Extensions;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using EventHub.Api.Models;
using EventHub.Domain.Authorization;
using EventHub.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(IEventsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<EventSummary>>> GetAll()
    {
        var events = await service.GetAllEventsAsync();
        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDetail>> GetById(Guid id)
    {
        try
        {
            var result = await service.GetEventByIdAsync(id);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost]
    [Authorize(Roles = RoleName.Organizer)]
    public async Task<ActionResult<EventSummary>> Create([FromBody] CreateEvent request)
    {
        try
        {
            var result = await service.CreateEventAsync(
                request.Title,
                request.Description,
                request.Location,
                request.StartAt,
                request.DoorsOpenAt,
                request.MaxParticipants,
                User.GetUserId(),
                request.CategoryId
            );

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (BusinessRuleException e)
        {
            return BadRequest(e.Message);
        }
        catch (AlreadyExistsException e)
        {
            return Conflict(e.Message);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleName.Organizer)]
    public async Task<ActionResult<EventDetail>> Update(Guid id, [FromBody] UpdateEvent request)
    {
        try
        {
            var result = await service.UpdateEventAsync(
                id,
                request.Title,
                request.Description,
                request.Location,
                request.StartAt,
                request.DoorsOpenAt,
                request.MaxParticipants,
                request.CategoryId,
                User.GetUserId()
            );

            return Ok(result);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (BusinessRuleException e)
        {
            return BadRequest(e.Message);
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleName.Organizer)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await service.CancelEventAsync(id, User.GetUserId());
            return NoContent();
        }
        catch (NotFoundException e)
        {
            Console.WriteLine(e);
            return NotFound(e.Message);
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
    }

  
}