using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventHub.Infrastructure.BackgroundJobs;

/// <summary>
/// The image is uploaded before the event is created. If the create fails or the form is
/// abandoned, the image stays in the bucket with no event pointing to it. This removes such
/// images once they are older than the grace period.
/// Images of cancelled events are still referenced and therefore kept.
/// </summary>
public sealed class OrphanImageCleaner(
    EventHubDbContext db,
    IImageStorage storage,
    TimeProvider timeProvider,
    IOptions<ImageCleanupOptions> options,
    ILogger<OrphanImageCleaner> logger)
{
    public async Task<int> RunAsync(CancellationToken ct)
    {
        var cutoff = timeProvider.GetUtcNow() - TimeSpan.FromHours(options.Value.GracePeriodHours);
        var referenced = (await db.Events
                .Where(e => e.ImageUrl != null)
                .Select(e => e.ImageUrl!)
                .ToListAsync(ct))
            .ToHashSet(StringComparer.Ordinal);

        var deleted = 0;
        await foreach (var image in storage.ListAsync(ct))
        {
            if (image.LastModified >= cutoff || referenced.Contains(image.Url)) continue;

            await storage.DeleteAsync(image.Url, ct);
            deleted++;
        }

        if (deleted > 0) logger.LogInformation("Deleted {Count} orphaned images", deleted);
        return deleted;
    }
}
