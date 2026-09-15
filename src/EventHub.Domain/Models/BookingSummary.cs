namespace EventHub.Domain.Models;

public class BookingSummary
{
    public required Guid BookingId { get; init; }
    public required Guid UserId { get; init; }
    public required string DisplayName { get; init; }
    public required DateTimeOffset BookedAt { get; init; }
}