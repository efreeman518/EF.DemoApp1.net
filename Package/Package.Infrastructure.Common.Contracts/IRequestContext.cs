namespace Package.Infrastructure.Common.Contracts;

public interface IRequestContext<out TAuditIdType>
{
    string CorrelationId { get; }
    TAuditIdType AuditId { get; }
    string? TenantId { get; }
}
