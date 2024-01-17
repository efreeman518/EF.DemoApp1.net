// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using Functions.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Functions;

/// <summary>
/// For EventGrid custom (topic/domain) event subscriptions 
/// Azure - create EventGrid Topic (or Domain) 
/// debug local 
///     - VS Tunnel (must be public) or ngrok (./ngrok http http://localhost:7071), run local
///     - in Azure create EventGrid subscription with webhook using VS Dev Tunnels or ngrok url (https://[tunnelurl]/runtime/webhooks/EventGrid?functionName=EventGridTriggerCustom
///     - this currently registers the subscription in Azure without having to validate the endpoint (as in a normal httpendpoint subscription like EventGridController)
///     - run test that sends event to the EventGrid topic
/// Azure
///     - deploy to Azure and create EventGrid subscription with the target being the EventGridTriggerCustom Function
///     - run test that sends event to the EventGrid topic
///     
/// https://learn.microsoft.com/en-us/azure/event-grid/delivery-and-retry
/// </summary>
public class FunctionEventGridTriggerCustom(ILogger<FunctionEventGridTriggerCustom> logger, IConfiguration configuration,
    IOptions<Settings1> settings)
{
    //private readonly ILogger<FunctionEventGridTriggerCustom> _logger = loggerFactory.CreateLogger<FunctionEventGridTriggerCustom>();

    [Function(nameof(FunctionEventGridTriggerCustom))]
    public async Task Run([EventGridTrigger] EventGridEvent egEvent)
    {
        _ = configuration.GetHashCode();
        _ = settings.GetHashCode();

        logger.Log(LogLevel.Information, "EventGridTriggerCustom - Start {inputEvent}", JsonSerializer.Serialize(egEvent));

        _ = egEvent.Data?.ToString(); //extract from inputEvent  Encoding.UTF8.GetString(egEvent.Data);

        //await some service call
        await Task.CompletedTask;

        logger.Log(LogLevel.Information, "EventGridTriggerCustom - Finish {inputEvent}", JsonSerializer.Serialize(egEvent));
    }
}
