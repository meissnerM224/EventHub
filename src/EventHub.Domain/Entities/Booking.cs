using EventHub.Domain.Enums;

namespace EventHub.Domain.Entities;

public class Booking
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Event Event { get; init; } = null!;
    public Guid UserId { get; init; }
    public BookingStatus Status { get; set; }
    public DateTimeOffset BookedAt { get; init; }
    public DateTimeOffset? CancelledAt { get; set; }
}