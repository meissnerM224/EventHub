using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using Microsoft.AspNetCore.Mvc;

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
}