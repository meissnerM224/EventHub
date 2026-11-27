namespace EventHub.Domain.Models;

public class MyBooking
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required string EventTitle { get; init; }
    public required DateTimeOffset EventStartsAt { get; init; }
    public required bool IsCancelled { get; init; }
    public required DateTimeOffset BookedAt { get; init; }
}
