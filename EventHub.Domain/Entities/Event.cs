namespace EventHub.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string Location { get; set; }

    public required DateTime StartsAt { get; set; }
    public DateTime? DoorsOpenAt { get; set; }
    public int MaxParticipants { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}