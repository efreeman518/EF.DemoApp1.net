namespace Package.Infrastructure.Common.Contracts;

public class RequestContext<TAuditIdType, TTenantIdType>(string correlationId, TAuditIdType auditId, TTenantIdType? tenantId, List<string> roles) : IRequestContext<TAuditIdType, TTenantIdType>
{
    public string CorrelationId => correlationId;
    public TAuditIdType AuditId => auditId;
    public TTenantIdType? TenantId => tenantId; // Nullable to allow for no tenant context
    public List<string> Roles { get; } = roles; // List of roles associated with the request context
}
