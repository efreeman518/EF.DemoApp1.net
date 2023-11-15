namespace Package.Infrastructure.Common;

public class RequestContext(string correlationId, string auditId) : IRequestContext
{
    public string CorrelationId => correlationId;
    public string AuditId => auditId;
}
