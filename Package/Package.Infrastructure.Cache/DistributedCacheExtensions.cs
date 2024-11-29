//using Microsoft.Extensions.Caching.Distributed;
//using System.Text;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace Package.Infrastructure.Cache;
//public static class DistributedCacheExtensions
//{
//    public static Task SetAsync<T>(this IDistributedCache cache, string key, T value, CancellationToken cancellationToken = default)
//    {
//        return SetAsync(cache, key, value, new DistributedCacheEntryOptions(), cancellationToken);
//    }
//    public static Task SetAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options, CancellationToken cancellationToken = default)
//    {
//        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, CacheJsonSerializerOptions));
//        return cache.SetAsync(key, bytes, options, cancellationToken);
//    }
//    public static bool TryGetValue<T>(this IDistributedCache cache, string key, out T? value)
//    {
//        var val = cache.GetAsync(key).GetAwaiter().GetResult(); //async methods can not have out parameters
//        value = default;
//        if (val == null) return false;
//        value = JsonSerializer.Deserialize<T>(val, CacheJsonSerializerOptions);
//        return true;
//    }

//    private static readonly JsonSerializerOptions CacheJsonSerializerOptions = new()
//    {
//        PropertyNamingPolicy = null,
//        WriteIndented = false,
//        AllowTrailingCommas = false,
//        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
//    };
//}
