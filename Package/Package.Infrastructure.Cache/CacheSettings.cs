namespace Package.Infrastructure.Cache;

/// <summary>
/// FusionCache settings
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Different cache instances can be named and have different default settings
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Redis connection string name to look up in the configuration
    /// ConnectionString not currently used because it doesn't work with FusionCache backplane; must load config separately
    /// </summary>
    public string? RedisConfigurationSection { get; set; }

    /// <summary>
    /// Change in settings if and when the version of the backplane message needs to change
    /// https://github.com/ZiggyCreatures/FusionCache/blob/main/docs/Backplane.md#-wire-format-versioning
    /// </summary>
    public string BackplaneChannelName { get; set; } = "FusionCacheBackplane";

    /// <summary>
    /// The duration of the cache in minutes
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// The duration of the distributed cache entry
    /// </summary>
    public int DistributedCacheDurationMinutes { get; set; }

    /// <summary>
    /// How long will we use expired cache value if the factory is unable to provide an updated value
    /// </summary>
    public int FailSafeMaxDurationMinutes { get; set; }

    /// <summary>
    /// How long will we wait before trying to get a new value from the factory after a fail-safe expiration
    /// </summary>
    public int FailSafeThrottleDurationMinutes { get; set; }
}

public class RedisConfiguration
{
    public string EndpointUrl { get; set; } = null!;
    public int Port { get; set; }
    public string Password { get; set; } = null!;
}
