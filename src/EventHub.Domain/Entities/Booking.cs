using EventHub.Domain.Enums;

namespace EventHub.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTime? CancelledAt { get; set; }

    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}