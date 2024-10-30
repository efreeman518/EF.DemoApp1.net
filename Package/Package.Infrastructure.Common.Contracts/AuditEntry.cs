namespace Package.Infrastructure.Common.Contracts;

public class AuditEntry() : IMessage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// User/system/process Id responsible for the activity that caused the audit
    /// </summary>
    public required string AuditId { get; set; }
    public required string EntityType { get; set; }
    public required string EntityKey { get; set; }
    public AuditStatus Status { get; set; }
    public required string Action { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public string? Metadata { get; set; }
    public string? Error { get; set; }
}

