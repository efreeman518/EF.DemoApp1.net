using Package.Infrastructure.BackgroundServices.InternalMessageBroker;
using Package.Infrastructure.Common.Contracts;

namespace Application.MessageHandlers;

public sealed class AuditHandler : IMessageHandler<AuditEntry>
{
    public async Task HandleAsync(AuditEntry message, CancellationToken cancellationToken = default)
    {
        //await some work
        await Task.Delay(100, cancellationToken);
    }
}
