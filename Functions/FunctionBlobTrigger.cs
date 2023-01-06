using Functions.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functions;

public class FunctionBlobTrigger
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FunctionBlobTrigger> _logger;
    private readonly Settings1 _settings;
    private readonly IDatabaseService _dbService;

    public FunctionBlobTrigger(IConfiguration configuration, ILoggerFactory loggerFactory,
        IOptions<Settings1> settings, IDatabaseService dbService)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<FunctionBlobTrigger>();
        _settings = settings.Value;
        _dbService = dbService;
    }

    /// <summary>
    /// large blobs - dont want the fileContent as string
    /// </summary>
    /// <param name="fileContent"></param>
    /// <param name="fileName"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    [Function("BlobTrigger")]
    public void Run(
        [BlobTrigger("%BlobContainer%/{fileName}", Connection = "StorageBlob1")] string fileContent, string fileName)
    {

        _ = _configuration.GetHashCode();
        _ = _settings.GetHashCode();
        _ = _dbService.GetHashCode();

        //var logger = context.GetLogger<BlobTrigger>();
        _logger.Log(LogLevel.Information, "BlobTrigger - Start {FileName}", fileName);

        //do something
        //await _dbService.RestoreAsync(fileName);

        _logger.Log(LogLevel.Information, "BlobTrigger - Finish {FileName}", fileName);
    }

}
