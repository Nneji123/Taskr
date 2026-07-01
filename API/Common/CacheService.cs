using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace API.Common;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken ct = default);
}

public class RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var bytes = await cache.GetAsync(key, ct);
        if (bytes == null) return null;
        return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(bytes));
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(5) };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        await cache.SetAsync(key, bytes, options, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default) => await cache.RemoveAsync(key, ct);

    public async Task RemoveByPatternAsync(string pattern, CancellationToken ct = default)
    {
        var connectionField = cache.GetType().GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance);
        var connection = connectionField?.GetValue(cache) as StackExchange.Redis.ConnectionMultiplexer;
        if (connection == null) { logger.LogWarning("Cannot clear cache by pattern"); return; }

        var server = connection.GetServer(connection.GetEndPoints().First());
        foreach (var key in server.Keys(pattern: pattern))
            await cache.RemoveAsync(key!, ct);
    }
}
