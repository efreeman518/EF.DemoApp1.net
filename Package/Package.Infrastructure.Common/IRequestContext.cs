namespace Package.Infrastructure.Common;
public interface IRequestContext<out TAuditIdType>
{
    string CorrelationId { get; }
    TAuditIdType AuditId { get; }
    string? TenantId { get; }
}
