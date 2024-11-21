using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Package.Infrastructure.Common.Extensions;

namespace SampleApp.Api.Endpoints;

/// <summary>
/// https://learn.microsoft.com/en-us/azure/event-grid/receive-events
/// </summary>
public static class EventGridEndpoints
{
    public static void MapEventGridEndpoints(this IEndpointRouteBuilder group)
    {
        //[FromHeader(Name = "aeg-event-type")] string aegEventType = "SubscriptionValidation" / "Notification"
        group.MapPost("/", static async ([FromServices] ILoggerFactory loggerFactory,
             List<EventGridEvent>? events = null) =>
        {
            var logger = loggerFactory.CreateLogger("EventGridEndpoints");
            logger.LogInformation("Received events: {Events}", events?.SerializeToJson());
            return await Event(logger, events);
        })
        .AllowAnonymous();
    }

    private static async Task<IResult> Event(ILogger logger, List<EventGridEvent>? events = null)
    {
        if (events == null) return TypedResults.BadRequest($"Request body does not deserialize in to List<EventGridEvent>");

        //initial call from EventGrid upon creating a Subscription is a one-time validation of the target - complete the event subscription validation handshake
        if (events[0].TryGetSystemEventData(out object eventData) && eventData is SubscriptionValidationEventData subscriptionValidationEventData)
        {
            logger.LogInformation("Received SubscriptionValidation event data,  topic: {Topic}; validation code: {ValidationCode}", events[0].Topic, subscriptionValidationEventData.ValidationCode);
            return TypedResults.Ok(new { ValidationResponse = subscriptionValidationEventData.ValidationCode });
            //return TypedResults.Ok(new SubscriptionValidationResponse());
        }
        //subsequent calls from EventGrid for a subscription contain normal events for processing
        else
        {
            //process the events concurrently - should be fast to return an OK to event grid
            var tasks = events.Select(e => ProcessEventGridEvent(logger, e));
            await Task.WhenAll(tasks.ToArray());
            return TypedResults.Ok();
        }
    }

    private static async Task ProcessEventGridEvent(ILogger logger, EventGridEvent egEvent)
    {
        var eData = egEvent.Data?.ToString(); //extract from inputEvent Encoding.UTF8.GetString(egEvent.Data);
        logger.LogEvent("EventGridEvent", eData);

        //queue background work or await some service call
        await Task.CompletedTask;
    }
}
