using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using EventHub.Api.Models;
using EventHub.Domain.Exceptions;

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
                request.OrganizerId,
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
                request.CategoryId
            );

            return Ok( result);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (BusinessRuleException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await service.CancelEventAsync(id);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            Console.WriteLine(e);
            return NotFound(e.Message);
        }
    }
}