using Package.Infrastructure.Domain.Contracts;

namespace Package.Infrastructure.Domain;
public static class CollectionUtility
{
    /// <summary>
    /// Helper function to process a collection of items with a processing function,
    /// returning a Result indicating success or failure.
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="items"></param>
    /// <param name="processFunc"></param>
    /// <param name="failFast"></param>
    /// <returns></returns>
    public static DomainResult ProcessCollection<TItem>(IEnumerable<TItem>? items, Func<TItem, DomainResult> processFunc, bool failFast = true)
    {
        if (items == null) return DomainResult.Success();

        if (failFast)
        {
            foreach (var item in items)
            {
                var result = processFunc(item);
                if (result.IsFailure) return result; // Fail fast
            }
            return DomainResult.Success();
        }

        var results = new List<DomainResult>();
        foreach (var item in items)
        {
            results.Add(processFunc(item));
        }
        return DomainResult.Combine(results.ToArray());
    }

    /// <summary>
    /// Processes items in a DTO collection to create, update, or optionally remove entities.
    /// If a <paramref name="removeFunc"/> is provided, entities not present in the DTO collection will be removed.
    /// Otherwise, no entities are removed, allowing for partial updates.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity in the database.</typeparam>
    /// <typeparam name="TDto">The type of the data transfer object.</typeparam>
    /// <typeparam name="TId">The type of the identifier for both entity and DTO.</typeparam>
    /// <param name="dbCollection">The collection of entities from the database.</param>
    /// <param name="dtoCollection">The collection of DTOs with incoming data.</param>
    /// <param name="getDbId">A function to extract the ID from an entity.</param>
    /// <param name="getDtoId">A function to extract the ID from a DTO.</param>
    /// <param name="createFunc">A function to create a new entity from a DTO.</param>
    /// <param name="updateFunc">An optional function to update an existing entity from a DTO. Not needed for a pure m-m join entity with no other properties</param>
    /// <param name="removeFunc">An optional function to remove an entity.</param>
    /// <param name="failFast">If true, stops processing on the first failure.</param>
    /// <returns>A <see cref="DomainResult"/> indicating success or failure of the operations.</returns>
    public static DomainResult SyncCollectionWithResult<TEntity, TDto, TId>(
        ICollection<TEntity> dbCollection,
        ICollection<TDto> dtoCollection,
        Func<TEntity, TId> getDbId,
        Func<TDto, TId?> getDtoId,
        Func<TDto, DomainResult> createFunc,
        Func<TEntity, TDto, DomainResult>? updateFunc = null,
        Func<TEntity, DomainResult>? removeFunc = null,
        bool failFast = false)
        where TId : struct, IEquatable<TId>
    {
        dtoCollection ??= [];
        var results = new List<DomainResult>();
        var dbMap = dbCollection.ToDictionary(getDbId);

        // Process Creates and Updates
        foreach (var dto in dtoCollection)
        {
            DomainResult result = DomainResult.Success();
            var dtoId = getDtoId(dto);
            if (!dtoId.HasValue || dtoId.Value.Equals(default) || !dbMap.TryGetValue(dtoId.Value, out var entity))
            {
                result = createFunc(dto);
            }
            else
            {
                // Item is present in both db and dto, so it's an update.

                // Remove it from the map so it's not considered for deletion later.
                dbMap.Remove(dtoId.Value);
                if (updateFunc != null)
                {
                    result = updateFunc(entity, dto);
                }
            }

            if (!result.IsSuccess && failFast) return result;
            results.Add(result);
        }

        // Process Deletes if removeFunc is provided
        // Any entities remaining in dbMap were not in the dto collection, so they should be removed.
        if (removeFunc != null)
        {
            foreach (var entityToRemove in dbMap.Values)
            {
                var result = removeFunc(entityToRemove);
                if (!result.IsSuccess && failFast) return result;
                results.Add(result);
            }
        }

        return DomainResult.Combine(results.ToArray());
    }
}
