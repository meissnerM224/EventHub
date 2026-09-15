using EventHub.Domain.Models;

namespace EventHub.Domain.Interfaces;

public interface IBookingsService
{
    Task<BookingConfirmation> BookAsync(Guid eventId, Guid userId);
    Task CancelBookingAsync(Guid eventId, Guid userId);
    Task<IReadOnlyList<BookingSummary>> GetBookingsAsync(Guid eventId, Guid userId);
}