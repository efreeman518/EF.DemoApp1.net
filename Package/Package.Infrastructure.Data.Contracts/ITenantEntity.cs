namespace Package.Infrastructure.Data.Contracts;

public interface ITenantEntity : IEntityBase<Guid>
{
    public Guid TenantId { get; init; }
}
