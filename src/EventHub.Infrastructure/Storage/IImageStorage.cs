namespace EventHub.Infrastructure.Storage;

public interface IImageStorage
{
    Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct);
}