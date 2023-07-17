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
///     - in Azure create EventGrid subscription with webhook using the ngrok url (https://087d-104-34-4-150.ngrok.io/runtime/webhooks/EventGrid?functionName=EventGridTriggerCustom
///     - this currently registers the subscription in Azure without having to validate the endpoint (as in a normal httpendpoint subscription like EventGridController)
///     - run test that sends event to the EventGrid topic
/// Azure
///     - deploy to Azure and create EventGrid subscription with the target being the EventGridTriggerCustom Function
///     - run test that sends event to the EventGrid topic
///     
/// https://learn.microsoft.com/en-us/azure/event-grid/delivery-and-retry
/// </summary>
public class FunctionEventGridTriggerCustom
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FunctionEventGridTriggerCustom> _logger;
    private readonly Settings1 _settings;

    public FunctionEventGridTriggerCustom(IConfiguration configuration, ILoggerFactory loggerFactory,
        IOptions<Settings1> settings)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<FunctionEventGridTriggerCustom>();
        _settings = settings.Value;
    }

    [Function("EventGridTriggerCustom")]
    public async Task Run([EventGridTrigger] EventGridEvent2 egEvent)
    {
        _ = _configuration.GetHashCode();
        _ = _settings.GetHashCode();

        _logger.Log(LogLevel.Information, "EventGridTriggerCustom - Start {inputEvent}", JsonSerializer.Serialize(egEvent));

        _ = egEvent.Data?.ToString(); //extract from inputEvent  Encoding.UTF8.GetString(egEvent.Data);

        //await some service call
        await Task.CompletedTask;

        _logger.Log(LogLevel.Information, "EventGridTriggerCustom - Finish {inputEvent}", JsonSerializer.Serialize(egEvent));
    }
}
