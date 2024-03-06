using Package.Infrastructure.Data.Contracts;

namespace Package.Infrastructure.Cache;

public static class CacheUtility
{
    /// <summary>
    /// Determine if <typeparamref name="T"/> is tenant specific (or its base type in case of List<typeparamref name="T"/>), if so, append tenantId to key
    /// Used by LazyCache/IAppCache extension methods and DistributedCacheManager
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public static string BuildCacheKey<T>(string key, string? tenantId = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        //determine if T is tenant specific, if so, append tenantId to key
        Type typeCheck = (typeof(T).IsGenericType && (typeof(T).GetGenericTypeDefinition() == typeof(List<>))) ? typeof(T).GetGenericArguments().Single() : typeof(T);
        //ITenantEntity from Package.Infrastructure.Data.Contracts, so package build order is important
        if (typeof(ITenantEntity).IsAssignableFrom(typeCheck))
        {
            ArgumentNullException.ThrowIfNull(tenantId);
            key = $"{tenantId}-{key}";
        }
        return key;
    }
}
