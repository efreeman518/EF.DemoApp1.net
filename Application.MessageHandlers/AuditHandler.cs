using Package.Infrastructure.BackgroundServices.InternalMessageBroker;
using Package.Infrastructure.Common.Contracts;

namespace Application.MessageHandlers;

public class AuditHandler : IMessageHandler<AuditEntry>
{
    public Task HandleAsync(AuditEntry message)
    {
        //await some work
        return Task.CompletedTask;
    }
}
