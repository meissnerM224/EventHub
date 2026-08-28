namespace EventHub.Domain.Models;

public class EventSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StarstAt { get; set; }
    public string Location { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int AvailableSpots{get; set;}
}