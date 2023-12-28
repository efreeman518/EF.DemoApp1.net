using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functions;

public class FunctionServiceBusQueue(ILogger<FunctionServiceBusQueue> logger, IConfiguration configuration, IOptions<Settings1> settings)
{
    //wire up a service bus then uncomment
    //[Function(nameof(FunctionServiceBusQueue))]
    public async Task Run([ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusQueue")] ServiceBusReceivedMessage message)
    {
        _ = configuration.GetHashCode();
        _ = settings.GetHashCode();

        logger.Log(LogLevel.Information, "ServiceBusQueueTrigger - Start MessageId: {MessageId} {Body}", message.MessageId, message.Body);

        //await some service call
        await Task.CompletedTask;

        logger.Log(LogLevel.Information, "ServiceBusQueueTrigger - Finish MessageId: {MessageId}", message.MessageId);

    }
}
