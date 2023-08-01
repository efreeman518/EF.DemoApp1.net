using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Messaging;

namespace Package.Infrastructure.Test.Integration.Messaging;
public class EventGridPublisher1 : EventGridPublisherBase, IEventGridPublisher1
{
    public EventGridPublisher1(ILogger<EventGridPublisher1> logger, IAzureClientFactory<EventGridPublisherClient> clientFactory, IOptions<EventGridPublisherSettings1> settings)
        : base(logger, clientFactory, settings)
    {

    }
}
