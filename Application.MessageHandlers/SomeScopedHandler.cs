using Application.Contracts.Interfaces;
using Package.Infrastructure.BackgroundServices.Attributes;
using Package.Infrastructure.BackgroundServices.InternalMessageBroker;
using Package.Infrastructure.Common.Contracts;

namespace Application.MessageHandlers;

[ScopedMessageHandler]
public class SomeScopedHandler(ITodoRepositoryTrxn repo) : IMessageHandler<AuditEntry>
{
    public async Task HandleAsync(AuditEntry message, CancellationToken cancellationToken = default)
    {
        //await some work
        _ = repo.GetHashCode();
        await Task.Delay(2000, cancellationToken);
    }
}
