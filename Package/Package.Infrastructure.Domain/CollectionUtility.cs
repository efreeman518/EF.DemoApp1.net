namespace Package.Infrastructure.Domain;
public static class CollectionUtility
{
    /// <summary>
    /// Synchronizes a collection of entities with a collection of DTOs,
    /// while correctly handling and propagating the Result pattern from domain operations.
    /// </summary>
    /// <typeparam name="TEntity">The type of the domain entity.</typeparam>
    /// <typeparam name="TDto">The type of the data transfer object.</typeparam>
    /// <typeparam name="TKey">The type of the key used for matching.</typeparam>
    /// <param name="existingEntities">The current collection of entities on the domain object.</param>
    /// <param name="incomingDtos">The collection of DTOs from the request.</param>
    /// <param name="entityKeySelector">A function to select the key from an entity.</param>
    /// <param name="dtoKeySelector">A function to select the key from a DTO.</param>
    /// <param name="createFunc">A function to create a new entity association. Must return a Result.</param>
    /// <param name="removeAction">An action to remove an entity association.</param>
    /// <param name="updateFunc">An optional function to update an existing entity association. Must return a Result.</param>
    /// <returns>A Result indicating the success or failure of the entire synchronization operation.</returns>
    public static Result SyncCollectionWithResult<TEntity, TDto, TKey>(
        ICollection<TEntity> existingEntities,
        ICollection<TDto>? incomingDtos,
        Func<TEntity, TKey> entityKeySelector,
        Func<TDto, TKey?> dtoKeySelector,
        Func<TDto, Result> createFunc,
        Action<TEntity> removeAction,
        Func<TEntity, TDto, Result>? updateFunc = null) where TKey : struct
    {
        if (incomingDtos == null)
        {
            foreach (var entity in existingEntities.ToList()) removeAction(entity);
            return Result.Success();
        }

        var existingDict = existingEntities.ToDictionary(entityKeySelector);
        var incomingDict = incomingDtos.Where(d => dtoKeySelector(d).HasValue).ToDictionary(d => dtoKeySelector(d)!.Value);

        // Remove entities that are in the existing collection but not in the incoming DTOs
        var keysToRemove = existingDict.Keys.Except(incomingDict.Keys).ToList();
        foreach (var key in keysToRemove)
        {
            removeAction(existingDict[key]);
        }

        // Add or Update entities
        foreach (var dto in incomingDtos)
        {
            var dtoKey = dtoKeySelector(dto);
            Result? operationResult;

            if (dtoKey.HasValue && existingDict.TryGetValue(dtoKey.Value, out var existingEntity))
            {
                // Update existing entity
                operationResult = updateFunc?.Invoke(existingEntity, dto);
            }
            else
            {
                // Create new entity
                operationResult = createFunc(dto);
            }

            if (operationResult != null && !operationResult.IsSuccess)
            {
                return operationResult; // Fail fast and propagate the error
            }
        }

        return Result.Success();
    }
}
