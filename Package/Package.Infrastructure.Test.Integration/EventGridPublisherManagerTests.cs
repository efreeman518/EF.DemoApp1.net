using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Messaging;

namespace Package.Infrastructure.Test.Integration;

[Ignore("Event Grid Topic setup required.")]

[TestClass]
public class EventGridPublisherManagerTests : IntegrationTestBase
{
    private readonly IEventGridPublisherManager _eventGridPublisherManager;

    public EventGridPublisherManagerTests()
    {
        _eventGridPublisherManager = Services.GetRequiredService<IEventGridPublisherManager>();
    }

    [TestMethod]
    public async Task SendEvent_pass()
    {
        var e = new EventGridEvent2(subject: "SubjectEntity", eventType: "DomainEvent1", dataVersion: "1.0",
          data: new
          {
              SubjectType = "SubjectType",
              SubjectId = $"{Guid.NewGuid()}",
              Event = "Something happened",
              Metadata = "whatever"
          });
        var evRequest = new EventGridRequest("EventGridPublisher1", e);
        var status = await _eventGridPublisherManager.SendAsync(evRequest);
        Assert.IsNotNull(status);
    }
}
