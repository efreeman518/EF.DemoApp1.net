using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using AutoMapper;
using Domain.Model;
using Domain.Shared.Enums;
using LazyCache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SampleApp.Bootstrapper.StartupTasks;
public class LoadCache(IConfiguration config, ILogger<LoadCache> logger, IAppCache appCache, IDistributedCache distCache, ITodoRepositoryQuery repoQuery, IMapper mapper) : IStartupTask
{
    private readonly IConfiguration _config = config;
    private readonly ILogger<LoadCache> _logger = logger;
    private readonly IAppCache _appCache = appCache;
    private readonly IDistributedCache _distCache = distCache;
    private readonly ITodoRepositoryQuery _repoQuery = repoQuery;
    private readonly IMapper _mapper = mapper;

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        _logger.Log(LogLevel.Information, "Startup LoadCache Start");
        try
        {
            _ = _config.GetHashCode();

            //memory cache
            var cacheSettings = await _repoQuery.QueryPageProjectionAsync<SystemSetting, SystemSettingDto>(_mapper.ConfigurationProvider,
                filter: s => (s.Flags & SystemSettings.MemoryCache) == SystemSettings.MemoryCache);
            cacheSettings.Data.ForEach(s => _appCache.Add(s.Key, s.Value));

            //distributed cache
            cacheSettings = await _repoQuery.QueryPageProjectionAsync<SystemSetting, SystemSettingDto>(_mapper.ConfigurationProvider,
                               filter: s => (s.Flags & SystemSettings.DistributedCache) == SystemSettings.DistributedCache);
            foreach (var s in cacheSettings.Data)
            {
                if (s.Value != null) await _distCache.SetStringAsync(s.Key, s.Value, new DistributedCacheEntryOptions(), cancellationToken);
            }

            _logger.Log(LogLevel.Information, "Startup LoadCache Finish");
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex, "Startup LoadCache Failed");
            throw; //stop app
        }
    }
}
