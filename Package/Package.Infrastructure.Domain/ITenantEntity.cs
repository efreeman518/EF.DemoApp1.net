namespace Package.Infrastructure.Domain;

public interface ITenantEntity<TTenantIdType> where TTenantIdType : struct
{
    public TTenantIdType TenantId { get; init; }
}
