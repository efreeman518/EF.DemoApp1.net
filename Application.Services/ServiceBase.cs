using Package.Infrastructure.Data.Contracts;

namespace Application.Services;

public abstract class ServiceBase
{
    protected readonly ILogger<ServiceBase> Logger;
    private readonly string? TenantId;

    protected ServiceBase(ILogger<ServiceBase> logger, string? tenantId = null)
    {
        Logger = logger;
        TenantId = tenantId;
    }
    protected string BuildCacheKey<T>(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        //determine if T is tenant specific, if so, append tenantId to key
        Type typeCheck = (typeof(T).IsGenericType && (typeof(T).GetGenericTypeDefinition() == typeof(List<>))) ? typeof(T).GetGenericArguments().Single() : typeof(T);
        //ITenantEntity from Package.Infrastructure.Data.Contracts, so package build order is important
        if (typeof(ITenantEntity).IsAssignableFrom(typeCheck))
        {
#pragma warning disable S3928 // Parameter names used into ArgumentException constructors should match an existing one 
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
            if (TenantId == null) throw new ArgumentNullException("TenantId");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
#pragma warning restore S3928 // Parameter names used into ArgumentException constructors should match an existing one 
            key = $"{TenantId}_{key}";
        }
        return key;
    }


}
