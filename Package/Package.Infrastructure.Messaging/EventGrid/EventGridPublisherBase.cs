using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Package.Infrastructure.Messaging.EventGrid;
public abstract class EventGridPublisherBase : IEventGridPublisher
{
    private readonly ILogger<EventGridPublisherBase> _logger;
    private readonly EventGridPublisherSettingsBase _settings;
    private readonly EventGridPublisherClient _egPublisherClient;

    protected EventGridPublisherBase(ILogger<EventGridPublisherBase> logger,
        IOptions<EventGridPublisherSettingsBase> settings, IAzureClientFactory<EventGridPublisherClient> clientFactory)

    {
        _logger = logger;
        _settings = settings.Value;
        _egPublisherClient = clientFactory.CreateClient(_settings.EventGridPublisherClientName);
    }

    public async Task<int> SendAsync(EventGridEvent egEvent, CancellationToken cancellationToken = default)
    {
        Azure.Messaging.EventGrid.EventGridEvent aegEvent = new(egEvent.Subject, egEvent.EventType, egEvent.DataVersion, egEvent.Data)
        {
            Id = egEvent.Id ?? Guid.NewGuid().ToString(),
            EventTime = egEvent.EventTime ?? DateTimeOffset.UtcNow,
            Topic = egEvent.Topic //must be null when publishing to a topic url; must be set when publishing to a domain url
        };

        _logger.LogInformation("SendAsync Start - {EventId} {Event}", aegEvent.Id, _settings.LogEventData ? JsonSerializer.Serialize(aegEvent.ToString()) : "LogEventData = false");
        var response = await _egPublisherClient.SendEventAsync(aegEvent, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        _logger.LogInformation("SendAsync Finish - {EventId}", aegEvent.Id);
        return response.Status;
    }
}
