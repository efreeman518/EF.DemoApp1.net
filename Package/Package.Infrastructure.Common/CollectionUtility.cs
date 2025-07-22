namespace Package.Infrastructure.Common;
public static class CollectionUtility
{
    public static void ProcessCollection<TItem>(IEnumerable<TItem>? items, Action<TItem> processAction, bool failFast = true)
    {
        if (items == null) return;

        if (failFast)
        {
            foreach (var item in items)
            {
                processAction(item);
            }
        }
        else
        {
            List<Exception>? exceptions = null;
            foreach (var item in items)
            {
                try
                {
                    processAction(item);
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }

            if (exceptions?.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }
    }

    /// <summary>
    /// Synchronizes two collections based on a key, performing create, update, and remove operations.
    /// This optimized version uses dictionaries for lookups, providing O(N+M) performance.
    /// </summary>
    /// <typeparam name="TBase">The type of items in the base collection (e.g., database entities).</typeparam>
    /// <typeparam name="TMod">The type of items in the modification collection (e.g., DTOs).</typeparam>
    /// <typeparam name="TKey">The type of the key used for matching items.</typeparam>
    /// <param name="baseCollection">The collection to be modified.</param>
    /// <param name="modCollection">The collection containing the desired state.</param>
    /// <param name="baseKeySelector">A function to extract the key from a base item.</param>
    /// <param name="modKeySelector">A function to extract the key from a modification item.</param>
    /// <param name="createAction">An action to create a new item in the base collection.</param>
    /// <param name="removeAction">An action to remove an item from the base collection.</param>
    /// <param name="updateAction">An optional action to update an existing item.</param>
    public static void SyncCollection<TBase, TMod, TKey>(
        ICollection<TBase> baseCollection,
        IEnumerable<TMod> modCollection,
        Func<TBase, TKey> baseKeySelector,
        Func<TMod, TKey> modKeySelector,
        Action<TMod> createAction,
        Action<TBase> removeAction,
        Action<TBase, TMod>? updateAction = null) where TKey : notnull
    {
        var baseDict = baseCollection.ToDictionary(baseKeySelector);
        var modDict = modCollection.ToDictionary(modKeySelector);

        // Items to remove: in base but not in mod
        var itemsToRemove = baseCollection
            .Where(baseItem => !modDict.ContainsKey(baseKeySelector(baseItem)))
            .ToList();
        foreach (var item in itemsToRemove)
        {
            removeAction(item);
        }

        // Items to add or update
        foreach (var modItem in modCollection)
        {
            var modKey = modKeySelector(modItem);
            if (baseDict.TryGetValue(modKey, out var baseItem))
            {
                // Item exists in both, so update
                updateAction?.Invoke(baseItem, modItem);
            }
            else
            {
                // Item is new, so create
                createAction(modItem);
            }
        }
    }

    /// <summary>
    /// Asynchronously synchronizes two collections based on a key, performing create, update, and remove operations.
    /// This optimized version uses dictionaries for lookups, providing O(N+M) performance.
    /// </summary>
    /// <typeparam name="TBase">The type of items in the base collection (e.g., database entities).</typeparam>
    /// <typeparam name="TMod">The type of items in the modification collection (e.g., DTOs).</typeparam>
    /// <typeparam name="TKey">The type of the key used for matching items.</typeparam>
    /// <param name="baseCollection">The collection to be modified.</param>
    /// <param name="modCollection">The collection containing the desired state.</param>
    /// <param name="baseKeySelector">A function to extract the key from a base item.</param>
    /// <param name="modKeySelector">A function to extract the key from a modification item.</param>
    /// <param name="createAction">An async function to create a new item in the base collection.</param>
    /// <param name="removeAction">An async function to remove an item from the base collection.</param>
    /// <param name="updateAction">An optional async function to update an existing item.</param>
    public static async Task SyncCollectionAsync<TBase, TMod, TKey>(
        ICollection<TBase> baseCollection,
        IEnumerable<TMod> modCollection,
        Func<TBase, TKey> baseKeySelector,
        Func<TMod, TKey> modKeySelector,
        Func<TMod, Task> createAction,
        Func<TBase, Task> removeAction,
        Func<TBase, TMod, Task>? updateAction = null) where TKey : notnull
    {
        var baseDict = baseCollection.ToDictionary(baseKeySelector);
        var modDict = modCollection.ToDictionary(modKeySelector);

        // Items to remove: in base but not in mod
        var itemsToRemove = baseCollection
            .Where(baseItem => !modDict.ContainsKey(baseKeySelector(baseItem)))
            .ToList();
        foreach (var item in itemsToRemove)
        {
            await removeAction(item);
        }

        // Items to add or update
        foreach (var modItem in modCollection)
        {
            var modKey = modKeySelector(modItem);
            if (baseDict.TryGetValue(modKey, out var baseItem))
            {
                // Item exists in both, so update
                if (updateAction != null)
                {
                    await updateAction(baseItem, modItem);
                }
            }
            else
            {
                // Item is new, so create
                await createAction(modItem);
            }
        }
    }
}
