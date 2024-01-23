using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Cache;
using Package.Infrastructure.Test.Integration.Model;

namespace Package.Infrastructure.Test.Integration;

[TestClass]
public class AppCacheTests : IntegrationTestBase
{
    private readonly IAppCache _cache;

    public AppCacheTests()
    {
        _cache = Services.GetRequiredService<IAppCache>();
    }

    private readonly TodoItemDto _someDto1 = new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = "Some name",
        Status = TodoItemStatus.Created
    };

    private readonly SomeTenantBasedDto _someTenantBasedDto1 = new()
    {
        TenantId = Guid.NewGuid(),
        Id = Guid.NewGuid(),
        Name = "Some name",
        Status = TodoItemStatus.Created
    };

    private async Task<TodoItemDto> GetSomeItemAsync()
    {
        await Task.Delay(100);
        return _someDto1;
    }

    private async Task<SomeTenantBasedDto> GetSomeTenantItemAsync()
    {
        await Task.Delay(100);
        return _someTenantBasedDto1;
    }

    [TestMethod]
    public async Task GetOrAddAsync_forceRefresh_RemoveAsync_pass()
    {
        var key = "some-cache-key";
        var cacheItem = await _cache.GetOrAddAsync(key, GetSomeItemAsync, tenantId: "SomeTenantId");
        Assert.AreEqual(_someDto1.Id, cacheItem?.Id);

        //again - step through to see that cache is hit
        cacheItem = await _cache.GetOrAddAsync(key, GetSomeItemAsync, tenantId: "SomeTenantId");
        Assert.AreEqual(_someDto1.Id, cacheItem?.Id);

        //force refresh - step through to see that cache is not hit, reload with function
        cacheItem = await _cache.GetOrAddAsync(key, GetSomeItemAsync, tenantId: "SomeTenantId", forceRefresh: true);
        Assert.AreEqual(_someDto1.Id, cacheItem?.Id);

        //remove from cache
        _cache.Remove<TodoItemDto>(key);
    }

    [TestMethod]
    public async Task TenantBased_GetOrAddAsync_forceRefresh_RemoveAsync_pass()
    {
        var key = "some-cache-key";
        var cacheItem = await _cache.GetOrAddAsync(key, GetSomeTenantItemAsync, tenantId: "SomeTenantId"); //force extension method to use tenantId
        Assert.AreEqual(_someTenantBasedDto1.Id, cacheItem?.Id);

        //again - step through to see that cache is hit
        cacheItem = await _cache.GetOrAddAsync(key, GetSomeTenantItemAsync, tenantId: "SomeTenantId", forceRefresh: false);
        Assert.AreEqual(_someTenantBasedDto1.Id, cacheItem?.Id);

        //force refresh - step through to see that cache is not hit, reload with function
        cacheItem = await _cache.GetOrAddAsync(key, GetSomeTenantItemAsync, tenantId: "SomeTenantId", forceRefresh: true);
        Assert.AreEqual(_someTenantBasedDto1.Id, cacheItem?.Id);

        //remove from cache
        _cache.Remove<SomeTenantBasedDto>(key, tenantId: "SomeTenantId");
    }

}
