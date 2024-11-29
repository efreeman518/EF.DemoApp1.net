//using LazyCache;
//using Microsoft.Extensions.DependencyInjection;
//using Package.Infrastructure.Cache;
//using Package.Infrastructure.Common;
//using Package.Infrastructure.Data.Contracts;
//using Package.Infrastructure.Test.Integration.Model;

//namespace Package.Infrastructure.Test.Integration;

//[Ignore("Tests for LazyCache")]

//[TestClass]
//public class AppCacheTests : IntegrationTestBase
//{
//    private readonly IAppCache _cache;
//    private const string TENANT_ID = "some-tenant-id";

//    public AppCacheTests()
//    {
//        _cache = Services.GetRequiredService<IAppCache>();
//    }

//    private readonly TodoItemDto _someDto1 = new()
//    {
//        Id = Guid.NewGuid().ToString(),
//        Name = "Some name",
//        Status = TodoItemStatus.Created
//    };

//    private readonly SomeTenantBasedDto _someTenantBasedDto1 = new()
//    {
//        TenantId = Guid.NewGuid(),
//        Id = Guid.NewGuid(),
//        Name = "Some name",
//        Status = TodoItemStatus.Created
//    };

//    private async Task<TodoItemDto> GetSomeItemAsync()
//    {
//        await Task.Delay(100);
//        return _someDto1;
//    }

//    private async Task<SomeTenantBasedDto> GetSomeTenantItemAsync()
//    {
//        await Task.Delay(100);
//        return _someTenantBasedDto1;
//    }

//    [TestMethod]
//    public async Task GetOrAddAsync_forceRefresh_RemoveAsync_pass()
//    {
//        //detemine cache key
//        var key = BuildCacheKey<TodoItemDto>("some-cache-key", TENANT_ID);
//        var cacheItem = await _cache.GetOrAddAsync(key, GetSomeItemAsync);
//        Assert.AreEqual(_someDto1.Id, cacheItem?.Id);

//        //again - step through to see that cache is hit
//        cacheItem = await _cache.GetOrAddAsync(key, GetSomeItemAsync);
//        Assert.AreEqual(_someDto1.Id, cacheItem?.Id);

//        //force refresh - step through to see that cache is not hit, reload with function
//        cacheItem = await _cache.GetOrAddAsync(key, GetSomeItemAsync, forceRefresh: true);
//        Assert.AreEqual(_someDto1.Id, cacheItem?.Id);

//        //remove from cache
//        _cache.Remove(key);
//    }

//    [TestMethod]
//    public async Task TenantBased_GetOrAddAsync_forceRefresh_RemoveAsync_pass()
//    {
//        var key = BuildCacheKey<TodoItemDto>("some-cache-key", TENANT_ID);
//        var cacheItem = await _cache.GetOrAddAsync(key, GetSomeTenantItemAsync); //force extension method to use tenantId
//        Assert.AreEqual(_someTenantBasedDto1.Id, cacheItem?.Id);

//        //again - step through to see that cache is hit
//        cacheItem = await _cache.GetOrAddAsync(key, GetSomeTenantItemAsync, forceRefresh: false);
//        Assert.AreEqual(_someTenantBasedDto1.Id, cacheItem?.Id);

//        //force refresh - step through to see that cache is not hit, reload with function
//        cacheItem = await _cache.GetOrAddAsync(key, GetSomeTenantItemAsync, forceRefresh: true);
//        Assert.AreEqual(_someTenantBasedDto1.Id, cacheItem?.Id);

//        //remove from cache
//        _cache.Remove(key);
//    }

//    /// <summary>
//    /// some local logic to consider tenant specific cache keys
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    /// <param name="key"></param>
//    /// <param name="tenantId"></param>
//    /// <returns></returns>
//    public static string BuildCacheKey<T>(string key, string? tenantId = null)
//    {
//        ArgumentNullException.ThrowIfNull(key);
//        //determine if T is tenant specific, if so, append tenantId to key

//        if (TypeUtility.IsTypeAssignable<T, ITenantEntity>())
//        {
//            ArgumentNullException.ThrowIfNull(tenantId);
//            key = $"{tenantId}-{key}";
//        }
//        return key;
//    }

//}
