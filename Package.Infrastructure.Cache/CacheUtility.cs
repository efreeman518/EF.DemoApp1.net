using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Package.Infrastructure.Data.Contracts;

namespace Package.Infrastructure.Cache;

public static class CacheUtility
{
    /// <summary>
    /// LazyCache/IAppCache extension method considering TenantId and force refresh
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="cache"></param>
    /// <param name="key"></param>
    /// <param name="factory"></param>
    /// <param name="tenantId"></param>
    /// <param name="cacheMinutes"></param>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public static async Task<T> GetOrAddAsync<T>(this IAppCache cache, string key, Func<Task<T>> factory,
        string? tenantId = null, int cacheMinutes = 20, bool forceRefresh = false)
    {
        key = BuildCacheKey<T>(key, tenantId);
        if (forceRefresh) cache.Remove(key);
        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = new TimeSpan(0, cacheMinutes, 0) };
        return await cache.GetOrAddAsync<T>(key, factory, options);
    }

    /// <summary>
    /// LazyCache/IAppCache extension method considering TenantId
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="cache"></param>
    /// <param name="key"></param>
    /// <param name="cancellationToken"></param>
    public static void Remove<T>(this IAppCache cache, string key, string? tenantId = null)
    {
        key = CacheUtility.BuildCacheKey<T>(key, tenantId);
        cache.Remove(key);
    }

    /// <summary>
    /// Used by LazyCache/IAppCache extension methods and DistributedCacheManager
    /// Determine if <typeparamref name="T"/> is tenant specific (or its base type in case of List<typeparamref name="T"/>), if so, append tenantId to key
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public static string BuildCacheKey<T>(string key, string? tenantId = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        //determine if T is tenant specific, if so, append tenantId to key
        Type typeCheck = (typeof(T).IsGenericType && (typeof(T).GetGenericTypeDefinition() == typeof(List<>))) ? typeof(T).GetGenericArguments().Single() : typeof(T);
        //ITenantEntity from Package.Infrastructure.Data.Contracts, so package build order is important
        if (typeof(ITenantEntity).IsAssignableFrom(typeCheck))
        {
            ArgumentNullException.ThrowIfNull(tenantId);
            key = $"{tenantId}-{key}";
        }
        return key;
    }
}
