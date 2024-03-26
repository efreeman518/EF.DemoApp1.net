namespace Package.Infrastructure.Common.Contracts;

public class RequestContext<TAuditIdType>(string correlationId, TAuditIdType auditId, string? tenantId = null) : IRequestContext<TAuditIdType>
{
    public string CorrelationId => correlationId;
    public TAuditIdType AuditId => auditId;
    public string? TenantId => tenantId;

}
