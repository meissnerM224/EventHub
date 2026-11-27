using System.Runtime.CompilerServices;
using Amazon.S3;
using Amazon.S3.Model;
using EventHub.Domain.Storage;

namespace EventHub.Infrastructure.Storage;

public sealed class S3ImageStorage(IAmazonS3 s3) : IImageStorage
{
    private const string UrlPrefix = $"/{ImagePath.Bucket}/";

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
        }, ct);
        return UrlPrefix + key;
    }

    public async IAsyncEnumerable<StoredImage> ListAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = ImagePath.Bucket,
            Prefix = $"{ImagePath.EventFolder}/",
        };
        ListObjectsV2Response response;
        do
        {
            response = await s3.ListObjectsV2Async(request, ct);
            foreach (var obj in response.S3Objects ?? [])
            {
                if (obj.LastModified is not { } modified) continue;
                yield return new StoredImage(UrlPrefix + obj.Key,
                    new DateTimeOffset(DateTime.SpecifyKind(modified.ToUniversalTime(), DateTimeKind.Utc)));
            }

            request.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated == true);
    }

    public Task DeleteAsync(string url, CancellationToken ct)
    {
        if (!url.StartsWith(ImagePath.EventImagePrefix, StringComparison.Ordinal))
            throw new ArgumentException($"Not an event image: {url}", nameof(url));
        return s3.DeleteObjectAsync(ImagePath.Bucket, url[UrlPrefix.Length..], ct);
    }
}
