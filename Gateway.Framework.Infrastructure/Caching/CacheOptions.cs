namespace Gateway.Framework.Infrastructure.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public bool Enabled { get; set; }
    public string Provider { get; set; } = "Memory";
    public int DefaultAbsoluteExpirationSeconds { get; set; } = 300;
    public string? RedisConnectionString { get; set; }
}
