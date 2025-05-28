namespace Package.Infrastructure.Data.Contracts;

public interface ITenantEntity<TTenantId>
{
    public TTenantId TenantId { get; init; }
}
