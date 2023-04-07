namespace Package.Infrastructure.Common;
public interface IRequestContext
{
    string CorrelationId { get; }
    string AuditId { get; }
}
