using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Messaging.EventGrid;

namespace Package.Infrastructure.Test.Integration.Messaging;
public class EventGridPublisher1(ILogger<EventGridPublisher1> logger, IOptions<EventGridPublisherSettings1> settings,
    IAzureClientFactory<EventGridPublisherClient> clientFactory) : EventGridPublisherBase(logger, settings, clientFactory), IEventGridPublisher1
{
}
