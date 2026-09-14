using EventHub.Domain.Entities;
using EventHub.Domain.Models;

namespace EventHub.Domain.Interfaces;

public interface IBookingRepository
{
    Task<IReadOnlyList<BookingSummary>> GetConfirmedBookingsAsync(Guid eventId);
    Task<bool> HasConfirmedBookingAsync(Guid eventId, Guid userId);
    Task<int> CountConfirmedAsync(Guid eventId);
    Task AddAsync(Booking booking);
    Task<Booking?> GetConfirmedBookingByIdAsync(Guid eventId, Guid userId);
    Task<bool> HasCancelledBookingAsync(Guid eventId, Guid userId);


    Task SaveAsync();
}