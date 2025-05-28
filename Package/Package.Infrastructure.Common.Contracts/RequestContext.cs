namespace Package.Infrastructure.Common.Contracts;

public class RequestContext<TAuditIdType, TTenantIdType>(string correlationId, TAuditIdType auditId, TTenantIdType? tenantId) : IRequestContext<TAuditIdType, TTenantIdType>
{
    public string CorrelationId => correlationId;
    public TAuditIdType AuditId => auditId;
    public TTenantIdType? TenantId => tenantId; // Nullable to allow for no tenant context
}
