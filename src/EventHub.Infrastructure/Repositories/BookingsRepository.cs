using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using EventHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Repositories;

public class BookingsRepository(EventHubDbContext context) : IBookingRepository
{
    public async Task<IReadOnlyList<BookingSummary>> GetConfirmedBookingsAsync(Guid eventId)
    {
        return await ConfirmedBookings(eventId)
            .Join(context.Users,
                booking => booking.UserId,
                user => user.Id,
                (booking, user) => new BookingSummary
                {
                    BookingId = booking.Id,
                    UserId = user.Id,
                    DisplayName = user.DisplayName,
                    BookedAt = booking.BookedAt
                })
            .OrderBy(summary => summary.BookedAt)
            .ToListAsync();
    }

    public Task<bool> HasConfirmedBookingAsync(Guid eventId, Guid userId)
        => ConfirmedBookings(eventId).AnyAsync(b => b.UserId == userId);


    public Task<int> CountConfirmedAsync(Guid eventId) => ConfirmedBookings(eventId).CountAsync();

    public async Task AddAsync(Booking booking)
    {
        context.Bookings.Add(booking);
        await SaveAsync();
    }

    public Task<Booking?> GetConfirmedBookingByIdAsync(Guid eventId, Guid userId)
        => ConfirmedBookings(eventId).FirstOrDefaultAsync(b => b.UserId == userId);

    public Task<bool> HasCancelledBookingAsync(Guid eventId, Guid userId)
    {
        return context.Bookings.AnyAsync(b =>
            b.EventId == eventId && b.UserId == userId && b.Status == BookingStatus.Cancelled);
    }


    public Task SaveAsync() => context.SaveChangesAsync();

    private IQueryable<Booking> ConfirmedBookings(Guid eventId) =>
        context.Bookings.Where(b => b.EventId == eventId && b.Status == BookingStatus.Confirmed);
}