using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Package.Infrastructure.BackgroundServices.InternalMessageBroker;
using Package.Infrastructure.Common.Attributes;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Common.Extensions;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Package.Infrastructure.Data.Interceptors;

/// <summary>
/// Registered as transient, but since the DbContext is pooled, this interceptor will be created once per DbContext instance.
/// https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors
/// </summary>
/// <param name="msgBus"></param>
public class AuditInterceptor<TAuditIdType, TTenantIdType>(IInternalMessageBus msgBus) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = false, ReferenceHandler = ReferenceHandler.IgnoreCycles };
    private readonly List<AuditEntry<TAuditIdType>> _auditEntries = [];
    private long _startTime;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        //handle nullable
        if (eventData.Context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        _startTime = Stopwatch.GetTimestamp();

        var changedEntries = eventData.Context.ChangeTracker.Entries()
            .Where(x => x.Entity is not AuditEntry<TAuditIdType> && x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();

        foreach (var entry in changedEntries)
        {
            //check props for mask
            var propInfo = entry.Entity.GetType().GetProperties();
            var maskedProps = propInfo.Where(pi => Attribute.GetCustomAttribute(pi, typeof(MaskAttribute)) != null).Select(pi => pi.Name).ToList();
            _auditEntries.Add(new AuditEntry<TAuditIdType>
            {
                AuditId = ((DbContextBase<TAuditIdType, TTenantIdType>)eventData.Context).AuditId,
                EntityType = entry.Metadata.DisplayName(), // .Entity.GetType().Name,
                EntityKey = entry.GetPrimaryKeyValues("N/A").SerializeToJson(jsonSerializerOptions, false)!,
                Action = entry.State.ToString(),
                Status = AuditStatus.Success,
                StartTime = TimeSpan.FromTicks(_startTime),
                Metadata = entry.State ==
                    EntityState.Modified ? entry.GetEntityChanges(maskedProps).PropertyChanges.SerializeToJson(jsonSerializerOptions) :
                    entry.State == EntityState.Added ? entry.Entity.SerializeToJson(jsonSerializerOptions) : null
            });
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_auditEntries.Count > 0)
            {
                var elapsed = Stopwatch.GetElapsedTime(_startTime);

                foreach (var auditEntry in _auditEntries)
                {
                    auditEntry.ElapsedTime = elapsed;
                }

                //publish the audit entries to the internal message broker and let a handler handle them
                msgBus.Publish(InternalMessageBusProcessMode.Topic, _auditEntries.ToList()); //ToList() makes a copy so when _auditEntries is cleared, the published entries are not affected

            }

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }
        finally
        {
            //clear the audit entries after saving changes since this is a pooled DbContext, does not behave like Transient
            _auditEntries.Clear();
        }

    }

    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        try
        {

            if (_auditEntries.Count > 0)
            {
                var elapsed = Stopwatch.GetElapsedTime(_startTime);

                foreach (var auditEntry in _auditEntries)
                {
                    auditEntry.ElapsedTime = elapsed;
                    auditEntry.Status = AuditStatus.Failure;
                    auditEntry.Error = eventData.Exception?.GetBaseException().Message;
                }

                //publish the audit entries to the internal message broker and let a handler handle them
                msgBus.Publish(InternalMessageBusProcessMode.Queue, _auditEntries.ToList()); //ToList() makes a copy so when _auditEntries is cleared, the published entries are not affected
            }

            await base.SaveChangesFailedAsync(eventData, cancellationToken);
        }
        finally
        {
            //clear the audit entries after saving changes since this is a pooled DbContext, does not behave like Transient
            _auditEntries.Clear();
        }
    }
}
