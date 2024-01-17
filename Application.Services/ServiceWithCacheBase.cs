using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Package.Infrastructure.Data.Contracts;

namespace Application.Services;

public abstract class ServiceBaseWithCache : ServiceBase
{
    protected readonly IAppCache Cache;
    private readonly string? TenantId;

    protected ServiceBaseWithCache(ILogger<ServiceBase> logger, IAppCache cache, string? tenantId = null)
        : base(logger)
    {
        Cache = cache;
        TenantId = tenantId;
    }

    protected async Task<T> CacheGetOrAddAsync<T>(string key, Func<Task<T>> factory, int cacheMinutes = 60, bool forceRefresh = false)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        key = BuildCacheKey<T>(key);
        if (forceRefresh) Cache.Remove(key);
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheMinutes)
        };
        return await Cache.GetOrAddAsync<T>(key, factory, options);
    }

    private string BuildCacheKey<T>(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        //determine if T is tenant specific, if so, append tenantId to key
        Type typeCheck = (typeof(T).IsGenericType && (typeof(T).GetGenericTypeDefinition() == typeof(List<>))) ? typeof(T).GetGenericArguments().Single() : typeof(T);
        //ITenantEntity from Package.Infrastructure.Data.Contracts, so package build order is important
        if (typeof(ITenantEntity).IsAssignableFrom(typeCheck))
        {
#pragma warning disable S3928 // Parameter names used into ArgumentException constructors should match an existing one 
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
            if (TenantId == null) throw new ArgumentNullException("TenantId");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
#pragma warning restore S3928 // Parameter names used into ArgumentException constructors should match an existing one 
            key = $"{TenantId}_{key}";
        }
        return key;
    }
}
