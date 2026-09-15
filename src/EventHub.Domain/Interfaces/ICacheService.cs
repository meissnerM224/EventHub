namespace EventHub.Domain.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiry) where T : class;
    public Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
}