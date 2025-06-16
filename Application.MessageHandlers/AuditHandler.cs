using Package.Infrastructure.BackgroundServices.InternalMessageBus;
using Package.Infrastructure.Common.Contracts;

namespace Application.MessageHandlers;

public sealed class AuditHandler : IMessageHandler<AuditEntry<string>>
{
    public async Task HandleAsync(AuditEntry<string> message, CancellationToken cancellationToken = default)
    {
        //await some work
        await Task.Delay(100, cancellationToken);
    }
}
