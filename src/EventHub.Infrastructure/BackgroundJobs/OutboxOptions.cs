namespace EventHub.Infrastructure.BackgroundJobs;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int PollIntervalMs { get; set; } = 5000;
    public int BatchSize { get; set; } = 20;
    public int MaxAttempts { get; set; } = 5;
}