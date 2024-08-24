using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Messaging.EventGrid;
using Package.Infrastructure.Test.Integration.Messaging;

namespace Package.Infrastructure.Test.Integration;

//[Ignore("Event Grid Topic setup required.")]

[TestClass]
public class EventGridPublisherTests : IntegrationTestBase
{
    private readonly IEventGridPublisher1 _eventGridPublisher;

    public EventGridPublisherTests()
    {
        _eventGridPublisher = Services.GetRequiredService<IEventGridPublisher1>();
    }

    [TestMethod]
    public async Task SendEvent_pass()
    {
        var e = new EventGridEvent(subject: "SubjectEntity", eventType: "DomainEvent1", dataVersion: "1.0",
          data: new
          {
              SubjectType = "SubjectType",
              SubjectId = $"{Guid.NewGuid()}",
              Event = "Something happened",
              Metadata = "whatever"
          });
        var status = await _eventGridPublisher.SendAsync(e);
        Assert.AreEqual(200, status);
    }
}
