namespace EventHub.Infrastructure.BackgroundJobs;

public sealed class ImageCleanupOptions
{
    public const string SectionName = "ImageCleanup";

    public int IntervalMinutes { get; set; } = 60;

    // An upload is only an orphan once nobody can still be filling in the form for it.
    public int GracePeriodHours { get; set; } = 24;
}
