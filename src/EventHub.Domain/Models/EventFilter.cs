namespace EventHub.Domain.Models;

public class EventFilter
{
    public int? CategoryId { get; init; }
    public string? Location { get; init; }
    public string? ImageUrl { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}