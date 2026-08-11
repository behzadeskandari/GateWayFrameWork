using System.Text.Json;
using Gateway.Framework.Shared.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Gateway.Framework.Infrastructure.Caching;

public sealed class DistributedCacheAdapter : ICache
{
    private readonly IDistributedCache _cache;
    private readonly CacheOptions _options;

    public DistributedCacheAdapter(IDistributedCache cache, IOptions<CacheOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var payload = await _cache.GetStringAsync(key, cancellationToken);
        return payload is null ? default : JsonSerializer.Deserialize<T>(payload, JsonDefaults.Options);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        var expiration = absoluteExpiration ??
                         TimeSpan.FromSeconds(_options.DefaultAbsoluteExpirationSeconds);

        var payload = JsonSerializer.Serialize(value, JsonDefaults.Options);
        await _cache.SetStringAsync(
            key,
            payload,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration },
            cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _cache.RemoveAsync(key, cancellationToken);
}
