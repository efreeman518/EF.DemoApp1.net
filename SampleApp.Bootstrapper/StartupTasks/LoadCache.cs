using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp.Bootstrapper.StartupTasks;
public class LoadCache : IStartupTask
{
    private readonly IConfiguration _config;
    private readonly ILogger<LoadCache> _logger;

    public LoadCache(IConfiguration config, ILogger<LoadCache> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        _logger.Log(LogLevel.Information, "Startup LoadCache Start");
        try
        {
            _ = _config.GetHashCode();

            //do something
            await Task.CompletedTask;

            _logger.Log(LogLevel.Information, "Startup LoadCache Finish");
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex, "Startup LoadCache Failed");
            throw; //stop app
        }
    }
}
