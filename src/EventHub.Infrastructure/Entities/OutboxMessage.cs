using System.ComponentModel.DataAnnotations;

namespace EventHub.Infrastructure.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public required string Type { get; init; }
    [MaxLength(5000)] public required string Payload { get; init; }
    public string? TraceId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }

    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}