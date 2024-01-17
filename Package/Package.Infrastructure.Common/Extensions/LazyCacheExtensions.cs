using LazyCache;

namespace Package.Infrastructure.Common.Extensions;

public static class LazyCacheExtensions
{
    public static Task<T> GetOrAddAsync2<T>(this IAppCache cache, string key, Func<Task<T>> factory, TimeSpan expire)
    {
        //build key

        return cache.GetOrAddAsync<T>(key, factory, expire);
    }
}
