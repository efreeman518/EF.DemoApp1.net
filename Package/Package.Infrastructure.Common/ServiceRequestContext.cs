using System;

namespace Package.Infrastructure.Common;

//this package will be a layer 1 nuget since other packages depend on it;
//those packages will be a higher layer (restored and built after layer 1)

public class ServiceRequestContext
{
    public string TraceId { get; }
    public string AuditId { get; }

    public ServiceRequestContext(string auditId, string? traceId = null)
    {
        AuditId = auditId;
        TraceId = traceId ?? Guid.NewGuid().ToString();
    }
}
