using EventHub.Api.Extensions;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class MeController(IBookingsService bookings) : ControllerBase
{
    [HttpGet("bookings")]
    [ProducesResponseType<IReadOnlyList<MyBooking>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<MyBooking>>> GetMyBookings()
    {
        return Ok(await bookings.GetMyBookingsAsync(User.GetUserId()));
    }
}
