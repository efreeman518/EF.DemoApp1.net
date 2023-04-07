namespace Package.Infrastructure.Common;

public class RequestContext : IRequestContext
{
    private readonly string _auditId;
    private readonly string _correlationId;

    public string CorrelationId => _correlationId; 
    public string AuditId => _auditId;

    public RequestContext(string correlationId, string auditId)
    {
        _correlationId = correlationId;
        _auditId = auditId;
    }
}
