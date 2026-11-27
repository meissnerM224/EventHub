using System.Reflection;
using EventHub.Infrastructure.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace EventHub.Api.Tests;

/// <summary>
/// The cache is best-effort. A Redis timeout (RedisTimeoutException derives from
/// TimeoutException, not RedisException) must behave like a cache miss, not like a 500.
/// </summary>
public sealed class RedisCacheServiceTests
{
    private sealed record Item(string Name);

    private static readonly RedisCacheService TimingOutCache = new(
        TimingOutRedis.Create(), NullLogger<RedisCacheService>.Instance);

    [Fact]
    public async Task GetAsync_ReturnsNull_OnTimeout() =>
        Assert.Null(await TimingOutCache.GetAsync<Item>("key"));

    [Fact]
    public Task SetAsync_DoesNotThrow_OnTimeout() =>
        TimingOutCache.SetAsync("key", new Item("x"), TimeSpan.FromMinutes(1));

    [Fact]
    public Task RemoveAsync_DoesNotThrow_OnTimeout() =>
        TimingOutCache.RemoveAsync("key");

    [Fact]
    public Task RemoveByPrefixAsync_DoesNotThrow_OnTimeout() =>
        TimingOutCache.RemoveByPrefixAsync("events:");

    // Every call on the multiplexer or the database throws a TimeoutException,
    // like a Redis that is reachable but too slow.
    public class TimingOutRedis : DispatchProxy
    {
        public static IConnectionMultiplexer Create() => Create<IConnectionMultiplexer, TimingOutRedis>();

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == nameof(IConnectionMultiplexer.GetDatabase))
                return Create<IDatabase, TimingOutRedis>();
            throw new TimeoutException($"Timeout performing {method?.Name}");
        }
    }
}
