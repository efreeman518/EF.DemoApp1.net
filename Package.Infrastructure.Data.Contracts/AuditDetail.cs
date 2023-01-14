namespace Package.Infrastructure.Data.Contracts;

public class AuditDetail : IAuditDetail
{
    private readonly string _auditId;

    public string AuditId => _auditId;
    public AuditDetail(string auditId)
    {
        _auditId = auditId;
    }
}
