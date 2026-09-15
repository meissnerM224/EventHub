namespace EventHub.Domain.Models;

public class BookingConfirmation
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required string EventTitle { get; init; }
    public required DateTimeOffset EventStartAt {  get; init; }
    public required DateTimeOffset BookedAt { get; init; }
}