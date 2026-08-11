using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Gateway.Framework.Infrastructure.Caching;

public sealed class MemoryCacheAdapter : ICache
{
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _options;

    public MemoryCacheAdapter(IMemoryCache cache, IOptions<CacheOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cache.TryGetValue(key, out T? value) ? value : default);

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        var expiration = absoluteExpiration ??
                         TimeSpan.FromSeconds(_options.DefaultAbsoluteExpirationSeconds);

        _cache.Set(key, value, expiration);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}
