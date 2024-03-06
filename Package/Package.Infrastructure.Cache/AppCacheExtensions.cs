using LazyCache;
using Microsoft.Extensions.Caching.Memory;

namespace Package.Infrastructure.Cache;

public static class AppCacheExtensions
{
    /// <summary>
    /// LazyCache/IAppCache extension method considering TenantId and forceRefresh
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
        key = CacheUtility.BuildCacheKey<T>(key, tenantId);
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

}
