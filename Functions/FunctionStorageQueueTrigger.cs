using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functions;

/// <summary>
/// If all five attempts fail, the functions runtime adds a message to a queue named <originalqueuename>-poison
/// https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger?tabs=python-v2%2Cin-process%2Cextensionv5&pivots=programming-language-csharp#poison-messages
/// </summary>
public class FunctionStorageQueueTrigger(ILogger<FunctionStorageQueueTrigger> logger, IConfiguration configuration, 
    IOptions<Settings1> settings)
{
    //private readonly ILogger<FunctionStorageQueueTrigger> _logger = loggerFactory.CreateLogger<FunctionStorageQueueTrigger>();

    [Function("QueueTrigger")]
    public async Task Run([QueueTrigger("%StorageQueueName%", Connection = "StorageQueue1")] string queueItem)
    {
        _ = configuration.GetHashCode();
        _ = settings.GetHashCode();

        logger.Log(LogLevel.Information, "StorageQueueTrigger - Start message: {queueItem}", queueItem);

        //await some service call
        await Task.CompletedTask;

        logger.Log(LogLevel.Information, "StorageQueueTrigger - Finish message: {queueItem}", queueItem);
    }
}
