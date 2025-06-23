namespace Package.Infrastructure.Common;
public static class CollectionUtility
{
    // Helper to synchronize collections
    public static void SyncCollection<TBase, TMod>(
        ICollection<TBase> baseCollection,
        IEnumerable<TMod> modCollection,
        Func<TBase, TMod, bool> matcher,
        Action<TMod> createAction,
        Action<TBase> removeAction,
        Action<TBase, TMod>? updateAction = null)
    {
        // Find items to remove
        var itemsToRemove = baseCollection
            .Where(baseItem => !modCollection.Any(modItem => matcher(baseItem, modItem)))
            .ToList();

        // Remove items
        foreach (var item in itemsToRemove)
        {
            removeAction(item);
        }

        // Add new items or update existing ones
        foreach (var modItem in modCollection)
        {
            var matchingItems = baseCollection.Where(baseItem => matcher(baseItem, modItem)).ToList();

            if (matchingItems.Count != 0)
            {
                updateAction?.Invoke(matchingItems[0], modItem);
            }
            else
            {
                createAction(modItem);
            }
        }
    }

    /// <summary>
    /// Asynchronously synchronizes two collections by performing add, update, and remove operations.
    /// </summary>
    /// <typeparam name="TBase">The type of items in the base collection</typeparam>
    /// <typeparam name="TMod">The type of items in the modification collection</typeparam>
    /// <param name="baseCollection">The collection to modify (database entities)</param>
    /// <param name="modCollection">The collection containing the desired state (DTOs)</param>
    /// <param name="matcher">Function to determine if items from both collections match</param>
    /// <param name="createAction">Async function to create a new item in the base collection</param>
    /// <param name="removeAction">Async function to remove an item from the base collection</param>
    /// <param name="updateAction">Optional async function to update an existing item</param>
    public static async Task SyncCollectionAsync<TBase, TMod>(
        ICollection<TBase> baseCollection,
        IEnumerable<TMod> modCollection,
        Func<TBase, TMod, bool> matcher,
        Func<TMod, Task> createAction,
        Func<TBase, Task> removeAction,
        Func<TBase, TMod, Task>? updateAction = null)
    {
        // Find items to remove
        var itemsToRemove = baseCollection
            .Where(baseItem => !modCollection.Any(modItem => matcher(baseItem, modItem)))
            .ToList();

        // Remove items
        foreach (var item in itemsToRemove)
        {
            await removeAction(item);
        }

        // Add new items or update existing ones
        foreach (var modItem in modCollection)
        {
            var matchingItems = baseCollection.Where(baseItem => matcher(baseItem, modItem)).ToList();

            if (matchingItems.Count != 0)
            {
                if (updateAction != null)
                {
                    await updateAction(matchingItems[0], modItem);
                }
            }
            else
            {
                await createAction(modItem);
            }
        }
    }

}
