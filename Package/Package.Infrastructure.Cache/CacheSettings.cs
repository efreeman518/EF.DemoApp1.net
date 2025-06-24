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
    /// Exclusive with RedisConfigurationSection
    /// </summary>
    public string? RedisConnectionStringName { get; set; }

    /// <summary>
    /// Section in the configuration
    /// if RedisConnectionStringName is null, this section may be used; Apparently Fusion Cache with Azure Redis has a problem using the connection string 
    /// </summary>
    public string? RedisConfigurationSection { get; set; }

    /// <summary>
    /// Change in settings if and when the version of the backplane message needs to change
    /// https://github.com/ZiggyCreatures/FusionCache/blob/main/docs/Backplane.md#-wire-format-versioning
    /// </summary>
    public string? BackplaneChannelName { get; set; }

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
