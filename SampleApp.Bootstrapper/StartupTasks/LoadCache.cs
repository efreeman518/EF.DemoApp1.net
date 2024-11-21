using Application.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace SampleApp.Bootstrapper.StartupTasks;
public class LoadCache(IConfiguration config, ILogger<LoadCache> logger, IFusionCacheProvider cache, ITodoRepositoryQuery repoQuery) : IStartupTask
{
    public async Task Execute(CancellationToken cancellationToken = default)
    {
        logger.Log(LogLevel.Information, "Startup LoadCache Start");

        _ = config.GetHashCode();
        _ = cache.GetHashCode();
        _ = repoQuery.GetHashCode();
        await Task.CompletedTask;

        ////memory cache
        //var cacheSettings = await repoQuery.QueryPageProjectionAsync(SystemSettingMapper.Projector,
        //    filter: s => (s.Flags & SystemSettings.MemoryCache) == SystemSettings.MemoryCache);
        ////cacheSettings.Data.ForEach(s => appCache.Add(s.Key, s.Value));
        //foreach (var item in cacheSettings.Data)
        //{
        //    appCache.Add(item.Key, item.Value);
        //}
        ////distributed cache
        //cacheSettings = await repoQuery.QueryPageProjectionAsync(SystemSettingMapper.Projector,
        //                   filter: s => (s.Flags & SystemSettings.DistributedCache) == SystemSettings.DistributedCache);
        //foreach (var s in cacheSettings.Data)
        //{
        //    if (s.Value != null) await distCache.SetStringAsync(s.Key, s.Value, new DistributedCacheEntryOptions(), cancellationToken);
        //}

        logger.Log(LogLevel.Information, "Startup LoadCache Finish");
    }
}
