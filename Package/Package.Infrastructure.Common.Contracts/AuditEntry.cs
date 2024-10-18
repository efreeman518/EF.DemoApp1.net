namespace Package.Infrastructure.Common.Contracts;

public class AuditEntry() : IMessage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// User/system/process Id responsible for the activity that caused the audit
    /// </summary>
    public required string AuditId { get; set; }
    public AuditStatus Status { get; set; }
    public required string Action { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string? Metadata { get; set; }
    public string? Error { get; set; }
}

