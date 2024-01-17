namespace Package.Infrastructure.Data.Contracts;

public interface ITenantEntity : IEntityBase
{
    public Guid TenantId { get; init; }
}
