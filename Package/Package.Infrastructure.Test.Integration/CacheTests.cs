using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Test.Integration.Model;
using ZiggyCreatures.Caching.Fusion;

namespace Package.Infrastructure.Test.Integration;

[Ignore("Redis setup required.")]

[TestClass]
public class CacheTests : IntegrationTestBase
{
    private readonly IFusionCache _cache;

    public CacheTests()
    {
        _cache = Services.GetRequiredService<IFusionCacheProvider>().GetCache("IntegrationTest.DefaultCache");
    }

    private static readonly TodoItemDto _someDto1 = new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = "Some name",
        Status = TodoItemStatus.Created
    };

    private static async Task<TodoItemDto> GetSomeItemAsync()
    {
        await Task.Delay(100);
        return _someDto1;
    }

    [TestMethod]
    public async Task GetOrAddAsync_RemoveAsync_pass()
    {
        string key = "some-cache-key";
        var cacheItem = await _cache.GetOrSetAsync(key, _ => GetSomeItemAsync());
        Assert.AreEqual(_someDto1.Id, cacheItem?.Id);

        //again - step through to see that cache is hit
        cacheItem = await _cache.GetOrSetAsync(key, _ => GetSomeItemAsync());
        Assert.AreEqual(_someDto1.Id, cacheItem?.Id);

        //force refresh 
        await _cache.RemoveAsync(key);
        //step through to see that cache is not hit, reload with factory and use custom settings this time
        var cacheOptions = new FusionCacheEntryOptions()
        {
            Duration = TimeSpan.FromMinutes(2),
            DistributedCacheDuration = TimeSpan.FromMinutes(10),
            FailSafeMaxDuration = TimeSpan.FromMinutes(10),
            FailSafeThrottleDuration = TimeSpan.FromSeconds(1),
            JitterMaxDuration = TimeSpan.FromSeconds(10),
            FactorySoftTimeout = TimeSpan.FromSeconds(1),
            FactoryHardTimeout = TimeSpan.FromSeconds(30),
            EagerRefreshThreshold = 0.9f
        };
        cacheItem = await _cache.GetOrSetAsync(key, _ => GetSomeItemAsync(), cacheOptions);
        Assert.AreEqual(_someDto1.Id, cacheItem?.Id);

        //remove from cache
        await _cache.RemoveAsync(key);

        //check cache is empty
        cacheItem = await _cache.GetOrDefaultAsync<TodoItemDto>(key);
        Assert.IsNull(cacheItem);
    }

}
