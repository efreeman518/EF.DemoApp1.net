//using LazyCache;
//using Microsoft.Extensions.Caching.Memory;

//namespace Package.Infrastructure.Cache;

//public static class AppCacheExtensions
//{
//    /// <summary>
//    /// LazyCache/IAppCache extension method considering forceRefresh
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    /// <param name="cache"></param>
//    /// <param name="key"></param>
//    /// <param name="factory"></param>
//    /// <param name="tenantId"></param>
//    /// <param name="cacheMinutes"></param>
//    /// <param name="forceRefresh"></param>
//    /// <returns></returns>
//    public static async Task<T> GetOrAddAsync<T>(this IAppCache cache, string key, Func<Task<T>> factory, int cacheMinutes = 20, bool forceRefresh = false)
//    {
//        if (forceRefresh) cache.Remove(key);
//        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = new TimeSpan(0, cacheMinutes, 0) };
//        return await cache.GetOrAddAsync<T>(key, factory, options);
//    }

//    /// <summary>
//    /// LazyCache/IAppCache extension method
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    /// <param name="cache"></param>
//    /// <param name="key"></param>
//    /// <param name="cancellationToken"></param>
//    public static void Remove(this IAppCache cache, string key)
//    {
//        cache.Remove(key);
//    }
//}
