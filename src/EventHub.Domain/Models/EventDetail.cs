namespace EventHub.Domain.Models;

public class EventDetail
{
    public required Guid EventId { get; init; }
    public required string EventTitle { get; init; }
    public required string EventDescription { get; init; }
    public required string EventLocation { get; init; }
    public required DateTimeOffset EventStartsAt { get; init; }
    public required DateTimeOffset DoorsOpenAt { get; init; }
    public required int MaxParticipants { get; init; }
    public required int AvailableSpots { get; init; }
    public required int CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public string? CategoryDescription { get; init; }
    public required Guid OrganizerId { get; init; }
    public required string OrganizerName { get; init; }
    public bool IsCancelled { get; init; }
}