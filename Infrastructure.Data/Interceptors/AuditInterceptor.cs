using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Package.Infrastructure.BackgroundServices.InternalMessageBroker;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Data.Contracts;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Data.Interceptors;

/// <summary>
/// https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors
/// </summary>
public class AuditInterceptor(IRequestContext<string> requestContext, IInternalBroker msgBroker) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = false, ReferenceHandler = ReferenceHandler.IgnoreCycles };
    private List<AuditEntry> _auditEntries = null!;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        //handle nullable
        if (eventData.Context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var startTime = DateTime.UtcNow;
        _auditEntries = eventData.Context.ChangeTracker.Entries()
            .Where(x => x.Entity is not AuditEntry && x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(x => new AuditEntry
            {
                AuditId = requestContext.AuditId,
                EntityType = x.Entity.GetType().Name,
                EntityId = x.Entity switch
                {
                    IEntityBase<int> entity => entity.Id.ToString(),
                    IEntityBase<Guid> entity => entity.Id.ToString(),
                    _ => "N/A"
                },
                Action = x.State.ToString(),
                Status = AuditStatus.Success,
                StartUtc = startTime,
                EndUtc = DateTime.UtcNow,
                Metadata = JsonSerializer.Serialize(x.Entity, jsonSerializerOptions)
            })
            .ToList();

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        //handle nullable
        if (eventData.Context is null)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        if (_auditEntries.Count > 0)
        {
            var endTime = DateTime.UtcNow;

            foreach (var auditEntry in _auditEntries)
            {
                auditEntry.EndUtc = endTime;
            }

            //save audit entries to database table - not ideal. Maybe outbox pattern?
            //eventData.Context.AddRange(_auditEntries);
            //_auditEntries.Clear();
            //await eventData.Context.SaveChangesAsync(cancellationToken);

            //publish the audit entries to the internal message broker and let a handler handle them
            //await publish the messages to the internal message broker
            msgBroker.Raise(InternalBrokerProcessMode.Queue, _auditEntries);

        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);

    }

    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        //handle nullable
        if (eventData.Context is null)
        {
            await base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        if (_auditEntries.Count > 0)
        {
            var endTime = DateTime.UtcNow;

            foreach (var auditEntry in _auditEntries)
            {
                auditEntry.EndUtc = endTime;
                auditEntry.Status = AuditStatus.Failure;
                auditEntry.Error = eventData.Exception?.GetBaseException().Message;
            }

            //save audit entries to database table - not ideal
            eventData.Context!.AddRange(_auditEntries);
            _auditEntries.Clear();
            await eventData.Context.SaveChangesAsync(cancellationToken);

            //publish the audit entries to the internal message broker and let a handler handle them
            //await publish the messages to the internal message broker

        }

        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }
}
