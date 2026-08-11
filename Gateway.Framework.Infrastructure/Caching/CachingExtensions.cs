using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gateway.Framework.Infrastructure.Caching;

public static class CachingExtensions
{
    public static IServiceCollection AddGatewayCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();

        if (string.Equals(options.Provider, "Redis", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(options.RedisConnectionString))
        {
            services.AddStackExchangeRedisCache(redis => redis.Configuration = options.RedisConnectionString);
            services.TryAddSingleton<ICache, DistributedCacheAdapter>();
        }
        else
        {
            services.AddMemoryCache();
            services.TryAddSingleton<ICache, MemoryCacheAdapter>();
        }

        return services;
    }
}
