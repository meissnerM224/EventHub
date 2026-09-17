using EventHub.Api.Extensions;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/events/{eventId:guid}/bookings")]
public class BookingsController(IBookingsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BookingSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BookingSummary>>> GetBookings(Guid eventId)
    {
        return Ok(await service.GetBookingsAsync(eventId, User.GetUserId()));
    }

    [HttpPost]
    [ProducesResponseType<BookingConfirmation>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingConfirmation>> Booking(Guid eventId)
    {
        var confirmation = await service.BookAsync(eventId, User.GetUserId());
        return StatusCode(StatusCodes.Status201Created, confirmation);
    }

    [HttpDelete("me")]
    [ProducesResponseType<ActionResult>(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteBooking(Guid eventId)
    {
        await service.CancelBookingAsync(eventId, User.GetUserId());
        return NoContent();
    }
}