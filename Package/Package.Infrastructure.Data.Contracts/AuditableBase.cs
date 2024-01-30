namespace Package.Infrastructure.Data.Contracts;

public abstract class AuditableBase<TId> : EntityBase, IAuditable<TId>
{
    protected AuditableBase()
    {
    }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public TId CreatedBy { get; set; } = default!;
    public DateTime? UpdatedDate { get; set; }
    public TId? UpdatedBy { get; set; }
}
