using Azure.Core.GeoJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
