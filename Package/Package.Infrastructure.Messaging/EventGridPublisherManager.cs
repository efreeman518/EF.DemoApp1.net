using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Package.Infrastructure.Messaging;

public class EventGridPublisherManager : IEventGridPublisherManager
{
    private readonly ILogger<EventGridPublisherManager> _logger;
    private readonly EventGridPublisherManagerSettings _settings;
    private readonly IAzureClientFactory<EventGridPublisherClient> _clientFactory;

    public EventGridPublisherManager(ILogger<EventGridPublisherManager> logger, IOptions<EventGridPublisherManagerSettings> settings,
        IAzureClientFactory<EventGridPublisherClient> clientFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _clientFactory = clientFactory;
    }

    public async Task<int> SendAsync(EventGridRequest request, CancellationToken cancellationToken = default)
    {
        EventGridEvent egEvent = new(request.Event.Subject, request.Event.EventType, request.Event.DataVersion, request.Event.Data)
        {
            Id = request.Event.Id ?? Guid.NewGuid().ToString(),
            EventTime = request.Event.EventTime ?? DateTimeOffset.UtcNow,
            Topic = request.Event.Topic //must be null when publishing to a topic url; must be set when publishing to a domain url
        };

        var client = _clientFactory.CreateClient(request.ClientName);
        _logger.LogInformation("SendAsync Start - {EventId} {Event}", egEvent.Id, _settings.LogEventData ? JsonSerializer.Serialize(egEvent.ToString()) : "LogEventData = false");
        var response = await client.SendEventAsync(egEvent, cancellationToken);
        _logger.LogInformation("SendAsync Finish - {EventId}", egEvent.Id);
        return response.Status;
    }
}
