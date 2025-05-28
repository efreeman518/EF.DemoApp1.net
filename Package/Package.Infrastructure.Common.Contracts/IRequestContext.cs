namespace Package.Infrastructure.Common.Contracts;

public interface IRequestContext<out TAuditIdType, out TTenantIdType>
{
    string CorrelationId { get; }
    TAuditIdType AuditId { get; }
    TTenantIdType? TenantId { get; } // Nullable to allow for no tenant context
}
