using Package.Infrastructure.BackgroundServices.InternalMessageBroker;
using Package.Infrastructure.Common.Contracts;

namespace Application.MessageHandlers;

public class AuditHandler : IMessageHandler<AuditEntry>
{
    public async Task HandleAsync(AuditEntry message)
    {
        //await some work
        await Task.Delay(2000);
    }
}
