using EventHub.Api.Extensions;
using EventHub.Domain.Exceptions;
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
    public async Task<ActionResult<IReadOnlyList<BookingSummary>>> GetBookings(Guid eventId)
    {
        try
        {
            return Ok(await service.GetBookingsAsync(eventId, User.GetUserId()));
        }
        catch (NotFoundException e)
        {
            Console.WriteLine(e);
            return NotFound();
        }
        catch (ForbiddenException e)
        {
            Console.WriteLine(e);
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<BookingConfirmation>> Booking(Guid eventId)
    {
        try
        {
            var confirmation = await service.BookAsync(eventId, User.GetUserId());
            return StatusCode(StatusCodes.Status201Created, confirmation);
        }
        catch (NotFoundException e)
        {
            return StatusCode(StatusCodes.Status404NotFound, e.Message);
        }
        catch (FullyBookedException e)
        {
            return Conflict(e.Message);
        }
    }

    [HttpDelete("me")]
    public async Task<ActionResult> DeleteBooking(Guid eventId)
    {
        try
        {
            await service.CancelBookingAsync(eventId, User.GetUserId());
            return NoContent();
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
}