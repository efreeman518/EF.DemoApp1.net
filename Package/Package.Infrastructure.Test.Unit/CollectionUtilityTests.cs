using Package.Infrastructure.Common;

namespace Package.Infrastructure.Test.Unit;

[TestClass]
public class CollectionUtilityTests
{
    [TestMethod]
    public void SyncCollection_with_null_modCollection_removes_existing_items()
    {
        var baseCollection = new List<TestItem>
        {
            new(1, "A"),
            new(2, "B")
        };

        CollectionUtility.SyncCollection<TestItem, TestItem, int>(
            baseCollection,
            modCollection: null!,
            baseKeySelector: item => item.Id,
            modKeySelector: item => item.Id,
            createAction: mod => baseCollection.Add(new TestItem(mod.Id, mod.Name)),
            removeAction: existing => baseCollection.Remove(existing),
            updateAction: (existing, mod) => existing.Name = mod.Name);

        Assert.AreEqual(0, baseCollection.Count);
    }

    [TestMethod]
    public async Task SyncCollectionAsync_with_null_modCollection_removes_existing_items()
    {
        var baseCollection = new List<TestItem>
        {
            new(1, "A"),
            new(2, "B")
        };

        await CollectionUtility.SyncCollectionAsync<TestItem, TestItem, int>(
            baseCollection,
            modCollection: null!,
            baseKeySelector: item => item.Id,
            modKeySelector: item => item.Id,
            createAction: mod =>
            {
                baseCollection.Add(new TestItem(mod.Id, mod.Name));
                return Task.CompletedTask;
            },
            removeAction: existing =>
            {
                baseCollection.Remove(existing);
                return Task.CompletedTask;
            },
            updateAction: (existing, mod) =>
            {
                existing.Name = mod.Name;
                return Task.CompletedTask;
            });

        Assert.AreEqual(0, baseCollection.Count);
    }

    [TestMethod]
    public void SyncCollection_creates_updates_and_removes_expected_items()
    {
        var baseCollection = new List<TestItem>
        {
            new(1, "one"),
            new(2, "two")
        };

        var modCollection = new List<TestItem>
        {
            new(2, "two-updated"),
            new(3, "three")
        };

        CollectionUtility.SyncCollection(
            baseCollection,
            modCollection,
            baseKeySelector: item => item.Id,
            modKeySelector: item => item.Id,
            createAction: mod => baseCollection.Add(new TestItem(mod.Id, mod.Name)),
            removeAction: existing => baseCollection.Remove(existing),
            updateAction: (existing, mod) => existing.Name = mod.Name);

        Assert.AreEqual(2, baseCollection.Count);
        Assert.IsFalse(baseCollection.Any(item => item.Id == 1));
        Assert.AreEqual("two-updated", baseCollection.Single(item => item.Id == 2).Name);
        Assert.AreEqual("three", baseCollection.Single(item => item.Id == 3).Name);
    }

    private sealed class TestItem(int id, string name)
    {
        public int Id { get; } = id;
        public string Name { get; set; } = name;
    }
}