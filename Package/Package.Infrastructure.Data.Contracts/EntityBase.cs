namespace Package.Infrastructure.Data.Contracts;

public class EntityBase : IEntityBase, IAuditable
{
    private readonly Guid _id = Guid.NewGuid();
    public EntityBase()
    {
    }

    public Guid Id
    {
        get { return _id; }
        init { if (value != Guid.Empty) _id = value; }
    }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "New";
    public DateTime UpdatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public byte[]? RowVersion { get; set; } //using as a shadow property causes concurrency problems when not tracked, so keep it on the base class
}
