using System.ComponentModel.DataAnnotations;

namespace EventHub.Domain.Models;

public class EventFilter
{
    public int? CategoryId { get; init; }
    // Same limit as Event.Location – a longer search can never match.
    [MaxLength(100)]
    public string? Location { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}