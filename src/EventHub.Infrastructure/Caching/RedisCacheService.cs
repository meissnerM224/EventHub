using System.Text.Json;
using EventHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EventHub.Infrastructure.Caching;

public class RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(key);
            if (value.IsNullOrEmpty) return null;
            var json = value.ToString();
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (RedisException e)
        {
            logger.LogWarning(e, "Cache red failed for {key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry) where T : class
    {
        try
        {
            await redis.GetDatabase().StringSetAsync(key, JsonSerializer.Serialize(value, Options), expiry);
        }
        catch (RedisException e)
        {
            logger.LogWarning(e, "Cache write failed for {Key}", key);
        }
    }


    public async Task RemoveAsync(string key)
    {
        try
        {
            await redis.GetDatabase().KeyDeleteAsync(key);
        }
        catch (RedisException e)
        {
            logger.LogWarning(e, "Cache remove failed for {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        try
        {
            var db = redis.GetDatabase();
            foreach (var endpoint in redis.GetEndPoints())
            {
                var server = redis.GetServer(endpoint);
                if (!server.IsConnected || server.IsReplica) continue;

                await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
                    await db.KeyDeleteAsync(key);
            }
        }
        catch (RedisException e)
        {
            logger.LogWarning(e, "Cache invalidation failed for {Prefix}", prefix);
        }
    }
}