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
/// For EventGrid Blob event subscriptions 
/// In Azure when creating the EventGrid subscription - filter the subject, otherwise all sorts of events are triggered:
///     - begins with /blobServices/default/containers/<containername>
///     - ends with .txt
/// debug local - https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local
///     - ngrok (./ngrok http -host-header=localhost 7071), run local (enables initial azure validation webhook handshake)
///     - in Azure create EventGrid subscription with webhook using VS Dev Tunnels or ngrok url (https://[tunnelurl]/runtime/webhooks/EventGrid?functionName=EventGridTriggerBlob
///     - this currently registers the subscription in Azure without normal http endpoint custom validation (as in a normal httpendpoint subscription like EventGridController)
///     - run test that creates event (upload blob)
/// Azure
///     - deploy to Azure and create EventGrid subscription with the target being the EventGridTriggerBlob
///     - run test that creates event (upload blob)
///     
/// https://learn.microsoft.com/en-us/azure/event-grid/delivery-and-retry
/// </summary>
public class FunctionEventGridTriggerBlob(ILogger<FunctionEventGridTriggerBlob> logger, IConfiguration configuration,
    IOptions<Settings1> settings)
{
    //private readonly ILogger<FunctionEventGridTriggerBlob> _logger = loggerFactory.CreateLogger<FunctionEventGridTriggerBlob>();

    [Function(nameof(FunctionEventGridTriggerBlob))]
    public async Task Run([EventGridTrigger] EventGridEvent inputEvent)
    {
        _ = configuration.GetHashCode();
        _ = settings.GetHashCode();

        string? fileName = Path.GetFileName(inputEvent.Subject);

        logger.Log(LogLevel.Information, "EventGridTriggerBlob - Start {fileName} {inputEvent}", fileName, JsonSerializer.Serialize(inputEvent));

        _ = inputEvent.Data?.ToString(); //extract from inputEvent

        //await some service call
        await Task.CompletedTask;

        logger.Log(LogLevel.Information, "EventGridTriggerBlob - Finish {fileName}", fileName);
    }
}
