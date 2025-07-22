namespace Package.Infrastructure.Domain;
public static class CollectionUtility
{
    /// <summary>
    /// Synchronizes a collection of entities with a collection of DTOs,
    /// while correctly handling and propagating the Result pattern from domain operations.
    /// This version is designed to handle variance in Result<T> return types.
    /// </summary>
    /// <typeparam name="TEntity">The type of the domain entity in the collection.</typeparam>
    /// <typeparam name="TDto">The type of the data transfer object.</typeparam>
    /// <param name="existingEntities">The current collection of entities on the domain object.</param>
    /// <param name="incomingDtos">The collection of DTOs from the request.</param>
    /// <param name="entityKeySelector">A function to select the key from an entity.</param>
    /// <param name="dtoKeySelector">A function to select the key from a DTO.</param>
    /// <param name="createFunc">A function to create a new entity association. Must return a Result.</param>
    /// <param name="removeAction">An action to remove an entity association.</param>
    /// <param name="updateFunc">An optional function to update an existing entity association. Must return a Result.</param>
    /// <param name="failFast">If true, the operation stops on the first failure. If false, it processes all items and aggregates errors.</param>
    /// <returns>A Result indicating the success or failure of the entire synchronization operation.</returns>
    public static Result SyncCollectionWithResult<TEntity, TDto>(
        ICollection<TEntity> existingEntities,
        ICollection<TDto>? incomingDtos,
        Func<TEntity, object> entityKeySelector,
        Func<TDto, object?> dtoKeySelector,
        Func<TDto, Result> createFunc,
        Action<TEntity> removeAction,
        Func<TEntity, TDto, Result>? updateFunc = null,
        bool failFast = true)
    {
        if (incomingDtos == null)
        {
            foreach (var entity in existingEntities.ToList()) removeAction(entity);
            return Result.Success();
        }

        var existingDict = existingEntities.ToDictionary(entityKeySelector);
        var incomingDict = incomingDtos
            .Select(dto => new { Dto = dto, Key = dtoKeySelector(dto) })
            .Where(x => x.Key != null)
            .ToDictionary(x => x.Key!, x => x.Dto);

        // Remove entities that are in the existing collection but not in the incoming DTOs
        foreach (var (key, entity) in existingDict)
        {
            if (!incomingDict.ContainsKey(key))
            {
                removeAction(entity);
            }
        }

        var operationResults = new List<Result>();

        // Add or Update entities
        foreach (var (key, dto) in incomingDict)
        {
            Result? operationResult;
            if (existingDict.TryGetValue(key, out var existingEntity))
            {
                // Update existing entity
                operationResult = updateFunc?.Invoke(existingEntity, dto);
            }
            else
            {
                // Create new entity
                operationResult = createFunc(dto);
            }

            if (operationResult == null) continue;

            if (failFast && operationResult.IsFailure)
            {
                return operationResult; // Fail fast and propagate the error
            }
            operationResults.Add(operationResult);
        }

        return failFast ? Result.Success() : Result.Combine([.. operationResults]);
    }
}
