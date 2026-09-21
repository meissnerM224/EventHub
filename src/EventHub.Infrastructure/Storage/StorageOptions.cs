namespace EventHub.Infrastructure.Storage;

public sealed class StorageOptions
{
    public string Endpoint { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
}