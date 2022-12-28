namespace Package.Infrastructure.Data.Contracts;

public class EntityBase : IAuditable
{
    public Guid Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime UpdatedDate { get; set; }
    public string? UpdatedBy { get; set; }
}
