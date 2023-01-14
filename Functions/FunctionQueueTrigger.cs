using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functions;

public class FunctionQueueTrigger
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FunctionQueueTrigger> _logger;
    private readonly Settings1 _settings;

    public FunctionQueueTrigger(IConfiguration configuration, ILoggerFactory loggerFactory,
        IOptions<Settings1> settings)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<FunctionQueueTrigger>();
        _settings = settings.Value;
    }

    [Function("QueueTrigger")]
    public async Task Run([QueueTrigger("%QueueName%", Connection = "StorageQueue1")] string queueItem)
    {
        _ = _configuration.GetHashCode();
        _ = _settings.GetHashCode();

        _logger.Log(LogLevel.Information, "QueueTrigger - Start message: {queueItem}", queueItem);

        //await some service call
        await Task.CompletedTask;

        _logger.Log(LogLevel.Information, "QueueTrigger - Finish message: {queueItem}", queueItem);
    }
}
