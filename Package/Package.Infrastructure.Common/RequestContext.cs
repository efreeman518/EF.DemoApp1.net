namespace Package.Infrastructure.Common;

public class RequestContext(string correlationId, string auditId, string? tenantId = null) : IRequestContext
{
    public string CorrelationId => correlationId;
    public string AuditId => auditId;
    public string? TenantId => tenantId;

}
