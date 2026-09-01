namespace EventHub.Domain.Entities;

public class Event
{
    public Guid Id { get; init; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string Location { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public required DateTimeOffset StartsAt { get; set; }
    public required DateTimeOffset DoorsOpenAt { get; set; }
    public int MaxParticipants { get; set; }


    public Guid OrganizerId { get; init; }
    public User Organizer { get; init; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public bool IsCancelled { get; set; } = false;
    public DateTimeOffset? CancelledAt { get; set; }
}