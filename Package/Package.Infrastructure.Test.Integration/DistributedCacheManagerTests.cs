using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Cache;
using Package.Infrastructure.Test.Integration.Model;

namespace Package.Infrastructure.Test.Integration;

[Ignore("Redis setup required.")]

[TestClass]
public class DistributedCacheManagerTests : IntegrationTestBase
{
    private readonly IDistributedCacheManager _cache;

    public DistributedCacheManagerTests()
    {
        _cache = Services.GetRequiredService<IDistributedCacheManager>();
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
        var cacheItem = await _cache.GetOrAddAsync(key, GetSomeItemAsync);
        Assert.AreEqual(_someDto1.Id, cacheItem?.Id);

        //again - step through to see that cache is hit
        cacheItem = await _cache.GetOrAddAsync(key, GetSomeItemAsync);
        Assert.AreEqual(_someDto1.Id, cacheItem?.Id);

        //force refresh - step through to see that cache is not hit, reload with function
        cacheItem = await _cache.GetOrAddAsync(key, GetSomeItemAsync, forceRefresh: true);
        Assert.AreEqual(_someDto1.Id, cacheItem?.Id);

        //remove from cache
        await _cache.RemoveAsync<TodoItemDto>(key);
    }

    [TestMethod]
    public async Task TenantBased_GetOrAddAsync_forceRefresh_RemoveAsync_pass()
    {
        var key = "some-cache-key";
        var cacheItem = await _cache.GetOrAddAsync(key, GetSomeTenantItemAsync);
        Assert.AreEqual(_someTenantBasedDto1.Id, cacheItem?.Id);

        //again - step through to see that cache is hit
        cacheItem = await _cache.GetOrAddAsync(key, GetSomeTenantItemAsync);
        Assert.AreEqual(_someTenantBasedDto1.Id, cacheItem?.Id);

        //force refresh - step through to see that cache is not hit, reload with function
        cacheItem = await _cache.GetOrAddAsync(key, GetSomeTenantItemAsync, forceRefresh: true);
        Assert.AreEqual(_someTenantBasedDto1.Id, cacheItem?.Id);

        //remove from cache
        await _cache.RemoveAsync<SomeTenantBasedDto>(key);
    }

}
