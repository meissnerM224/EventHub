using Amazon.S3;
using Amazon.S3.Model;
using EventHub.Domain.Storage;
using Microsoft.Extensions.Options;

namespace EventHub.Infrastructure.Storage;

public sealed class S3ImageStorage(IAmazonS3 s3, IOptions<StorageOptions> options) : IImageStorage
{
    private readonly StorageOptions _options = options.Value;

    public async Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct)
    {
        var key = $"{ImagePath.EventFolder}/{Guid.NewGuid():N}.webp";
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = ImagePath.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            UseChunkEncoding = false,
            // DisablePayloadSigning = true
        }, ct);
        return $"/{ImagePath.Bucket}/{key}";
    }
}