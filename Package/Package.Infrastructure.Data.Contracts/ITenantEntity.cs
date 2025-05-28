namespace Package.Infrastructure.Data.Contracts;

public interface ITenantEntity<TTenantIdType>
{
    public TTenantIdType TenantId { get; init; }
}
