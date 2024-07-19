namespace Package.Infrastructure.Cache;

public interface IDistributedCacheManager
{
    Task<T?> GetOrAddAsync<T>(string key, Func<Task<T>> factory, int cacheMinutes = 60, bool forceRefresh = false, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync<T>(string key, CancellationToken cancellationToken = default);
}
