using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functions;

public class FunctionServiceBusTopic(ILogger<FunctionServiceBusTopic> logger, IConfiguration configuration, IOptions<Settings1> settings)
{
    //wire up a service bus then uncomment
    //[Function(nameof(FunctionServiceBusTopic))]
    public async Task Run([ServiceBusTrigger("%ServiceBusTopicName%", "%ServiceBusSubscriptionName%", Connection = "ServiceBusTopic")] ServiceBusReceivedMessage message)
    {
        _ = configuration.GetHashCode();
        _ = settings.GetHashCode();

        logger.Log(LogLevel.Information, "ServiceBusTopicTrigger - Start MessageId: {MessageId} {Body}", message.MessageId, message.Body);

        //await some service call
        await Task.CompletedTask;

        logger.Log(LogLevel.Information, "ServiceBusTopicTrigger - Finish MessageId: {MessageId}", message.MessageId);
    }
}
