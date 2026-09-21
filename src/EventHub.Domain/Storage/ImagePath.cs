namespace EventHub.Domain.Storage;

public static class ImagePath
{
    public const string Bucket = "media";
    public const string EventFolder = "events";
    public const string EventImagePrefix = $"/{Bucket}/{EventFolder}/";
}