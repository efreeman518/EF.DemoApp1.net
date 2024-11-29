//using Microsoft.Extensions.Caching.Distributed;
//using Microsoft.Extensions.Logging;
//using Package.Infrastructure.Common.Extensions;

//namespace Package.Infrastructure.Cache;

///// <summary>
///// 
///// </summary>
///// <param name="logger"></param>
///// <param name="distCache"></param>
//public class DistributedCacheManager(ILogger<DistributedCacheManager> logger, IDistributedCache distCache)
//    : IDistributedCacheManager
//{
//    private static readonly SemaphoreSlim _semaphore = new(1, 1);

//    public async Task<T?> GetOrAddAsync<T>(string key, Func<Task<T>> factory, int cacheMinutes = 60,
//        bool forceRefresh = false, CancellationToken cancellationToken = default) where T : class
//    {
//        ArgumentNullException.ThrowIfNull(key);
//        ArgumentNullException.ThrowIfNull(factory);
//        if (forceRefresh) await distCache.RemoveAsync(key, cancellationToken);
//        logger.DebugLog($"GetOrAddAsync - Cache key: {key}, forceRefresh:{forceRefresh}.");

//        //var result = !forceRefresh ? await distCache.GetStringAsync(key, cancellationToken) : null;
//        if (forceRefresh || !distCache.TryGetValue(key, out T? cacheItem))
//        {
//            try
//            {
//                //lock to prevent multiple threads from running factory()
//                await _semaphore.WaitAsync(cancellationToken); //enters if semaphore > 0 

//                //check again in case another thread/process added the same key to the cache
//                if (distCache.TryGetValue(key, out cacheItem))
//                {
//                    logger.InfoLog($"GetOrAddAsync {key} found in semaphore/critical block.");
//                }
//                else
//                {
//                    logger.InfoLog($"GetOrAddAsync {key} still not found in critical block, running factory.");
//                    cacheItem = await factory();
//                    if (cacheItem != null)
//                    {
//                        logger.InfoLog($"GetOrAddAsync {key} factory retrieved value.");
//                        var options = new DistributedCacheEntryOptions
//                        {
//                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheMinutes)
//                        };
//                        await distCache.SetAsync(key, cacheItem, options, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
//                    }
//                }
//            }
//            finally
//            {
//                _semaphore.Release(); //semaphore +1
//                logger.DebugLog($"GetOrAddAsync {key} semaphore released.");
//            }
//        }
//        if (cacheItem == default(T))
//        {
//            logger.InfoLog($"GetOrAddAsync {key} not found and factory returned null; returning default<T>.");
//            return default;
//        }
//        logger.DebugLog($"GetOrAddAsync Finish - {key}");
//        return cacheItem;
//    }

//    public async Task RemoveAsync<T>(string key, CancellationToken cancellationToken = default)
//    {
//        logger.InfoLog($"RemoveAsync - Cache key: {key}.");
//        await distCache.RemoveAsync(key, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
//    }
//}
