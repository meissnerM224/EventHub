namespace EventHub.Infrastructure.Storage;

public interface IImageStorage
{
    Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct);

    // All stored event images, as the same "/media/events/..." URL that SaveAsync returns.
    IAsyncEnumerable<StoredImage> ListAsync(CancellationToken ct);

    Task DeleteAsync(string url, CancellationToken ct);
}

public sealed record StoredImage(string Url, DateTimeOffset LastModified);
