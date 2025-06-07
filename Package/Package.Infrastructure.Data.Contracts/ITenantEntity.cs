namespace Package.Infrastructure.Data.Contracts;

public interface ITenantEntity<TTenantIdType> where TTenantIdType : struct
{
    public TTenantIdType TenantId { get; init; }
}
