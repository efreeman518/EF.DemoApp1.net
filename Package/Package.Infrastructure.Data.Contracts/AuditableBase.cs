namespace Package.Infrastructure.Data.Contracts;

public abstract class AuditableBase<TAuditIdType> : EntityBase, IAuditable<TAuditIdType>
{
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public TAuditIdType CreatedBy { get; set; } = default!; //must be set by the caller
    public DateTime? UpdatedDate { get; set; }
    public TAuditIdType? UpdatedBy { get; set; }
}
