namespace EventHub.Domain.Models;

public class EventSummary
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required DateTimeOffset StartsAt { get; init; }
    public required string Location { get; init; }
    public required string CategoryName { get; init; }
    public required int AvailableSpots { get; init; }
}