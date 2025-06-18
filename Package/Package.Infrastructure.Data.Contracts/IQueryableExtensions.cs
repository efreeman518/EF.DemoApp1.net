using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Package.Infrastructure.Common.Contracts;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Package.Infrastructure.Data.Contracts;
public static class IQueryableExtensions
{
    private static readonly ConcurrentDictionary<Type, object?> typeDefaults = new();

    /// <summary>
    /// Returns the IQueryable for further composition or streaming; 
    /// client code expected to subsequently call ToListAsync() on the IQueryable<T> to return (paged) results,
    /// or GetStream/GetStreamProjection which returns IAsyncEnumerable<T> for streaming
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="query"></param>
    /// <param name="tracking"></param>
    /// <param name="pageSize">If null, then return IQueryable for streaming (no paging)</param>
    /// <param name="pageIndex">1-based; If null, then return IQueryable for streaming (no paging)</param>
    /// <param name="filter"></param>
    /// <param name="orderBy"></param>
    /// <param name="splitQuery">Discretionary; avoid cartesian explosion, applicable with Includes; understand the risks/repercussions (when paging, etc) of using this https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries</param>
    /// <param name="includes"></param>
    /// <returns></returns>
    public static IQueryable<T> ComposeIQueryable<T>(this IQueryable<T> query, bool tracking = false,
        int? pageSize = null, int? pageIndex = null,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool splitQuery = false,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        //https://dotnetdocs.ir/Post/45/the-difference-between-asnotracking-and-asnotrackingwithidentityresolution
        query = tracking ? query.AsTracking() : query.AsNoTrackingWithIdentityResolution();

        //Where
        if (filter != null) query = query.Where(filter);

        //Order - required for paging (Skip)
        if (orderBy != null) query = orderBy(query);

        //Paging
        if (pageSize != null && pageIndex != null)
        {
            int skipCount = (pageIndex.Value - 1) * pageSize.Value;
            query = skipCount == 0 ? query.Take(pageSize.Value) : query.Skip(skipCount).Take(pageSize.Value);
        }

        //Related Includes
        if (includes.Length > 0 && includes[0] != null)
        {
            includes.ToList().ForEach(include =>
            {
                query = include(query);
            });

            //Split (discretionary) reduces cartesian explosion when joining (multiple includes at the same level)
            if (splitQuery) query = query.AsSplitQuery();
        }

        return query;
    }

    /// <summary>
    /// Efficiently filter entities by a large collection of values for any property.
    /// Uses optimal strategy (IN clause or JOIN) based on collection size.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TKey">The property type (could be Guid, int, string, etc.)</typeparam>
    /// <param name="query">The source IQueryable</param>
    /// <param name="propertySelector">Expression to select the property from entity</param>
    /// <param name="values">Collection of values to filter by</param>
    /// <param name="threshold">Optional threshold to control when to switch from Contains to Join (default: 30)</param>
    /// <returns>Filtered IQueryable</returns>
    public static IQueryable<T> WherePropertyIn<T, TKey>(this IQueryable<T> query,
        Expression<Func<T, TKey>> propertySelector,
        ICollection<TKey> values,
        int threshold = 30)
        where T : class
    {
        if (values == null || values.Count == 0)
            return query.Take(0); // Return empty result if no values provided

        // For small sets, Contains is still efficient
        if (values.Count <= threshold)
        {
            return query.Where(BuildContainsPredicate(propertySelector, values));
        }

        // For large sets, use a join operation
        var valuesQuery = values.AsQueryable();

        // Convert propertySelector from t => t.Property to t => new { Property = t.Property }
        var parameter = propertySelector.Parameters[0];
        var property = propertySelector.Body;

        // Create lambda for the join
        var keySelector = Expression.Lambda<Func<T, TKey>>(property, parameter);

        // Use join operation (SQL will optimize this better than IN with large sets)
        return query.Join(
            valuesQuery,
            keySelector,
            value => value,
            (entity, _) => entity);
    }

    private static Expression<Func<T, bool>> BuildContainsPredicate<T, TKey>(
        Expression<Func<T, TKey>> idSelector,
        ICollection<TKey> ids)
    {
        // Create a Contains expression: entity => ids.Contains(entity.Id)
        var containsMethod = (typeof(Enumerable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "Contains" && m.GetParameters().Length == 2)?
            .MakeGenericMethod(typeof(TKey))) ?? throw new InvalidOperationException("Could not find Contains method");

        var idsConstant = Expression.Constant(ids);
        var call = Expression.Call(containsMethod, idsConstant, idSelector.Body);

        return Expression.Lambda<Func<T, bool>>(call, idSelector.Parameters);
    }

    /// <summary>
    /// Return IAsyncEnumerable for streaming - await foreach (var x in GetStream<Entity>(...).WithCancellation(cancellationTokenSource.Token))
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="query"></param>
    /// <param name="tracking"></param>
    /// <param name="filter"></param>
    /// <param name="orderBy"></param>
    /// <param name="splitQuery">Discretionary; avoid cartesian explosion, applicable with Includes; understand the risks/repercussions (when paging, etc) of using this https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries</param>
    /// <param name="includes"></param>
    /// <returns></returns>
    public static IAsyncEnumerable<T> GetStream<T>(this IQueryable<T> query, bool tracking = false, Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool splitQuery = false,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        return query.ComposeIQueryable(tracking, null, null, filter, orderBy, splitQuery, includes).AsAsyncEnumerable();
    }

    /// <summary>
    /// Return IAsyncEnumerable projection for streaming - await foreach (var x in GetStreamProjection<Entity, Dto>(...).WithCancellation(cancellationTokenSource.Token))
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TProject"></typeparam>
    /// <param name="query"></param>
    /// <param name="projector"></param>
    /// <param name="tracking"></param>
    /// <param name="filter"></param>
    /// <param name="orderBy"></param>
    /// <param name="splitQuery">Discretionary; avoid cartesian explosion, applicable with Includes; understand the risks/repercussions (when paging, etc) of using this https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries</param>
    /// <param name="includes"></param>
    /// <returns></returns>
    public static IAsyncEnumerable<TProject> GetStreamProjection<T, TProject>(this IQueryable<T> query, Func<T, TProject> projector,
        bool tracking = false, Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool splitQuery = false,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        return query.ComposeIQueryable(tracking, null, null, filter, orderBy, splitQuery, includes).Select(e => projector(e)).AsAsyncEnumerable();
    }

    public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, IEnumerable<Sort> sorts)
    {
        var expression = source.Expression;
        int count = 0;
        foreach (var sort in sorts)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var selector = Expression.PropertyOrField(parameter, sort.PropertyName);
            var method = sort.SortOrder == SortOrder.Descending ?
                (count == 0 ? "OrderByDescending" : "ThenByDescending") :
                (count == 0 ? "OrderBy" : "ThenBy");
            expression = Expression.Call(typeof(Queryable), method,
                [source.ElementType, selector.Type],
                expression, Expression.Quote(Expression.Lambda(selector, parameter)));
            count++;
        }
        return count > 0 ? source.Provider.CreateQuery<T>(expression) : source;
    }

    /// <summary>
    /// Very basic filter builder; inspect filterItem's properties and for any that are not the default value for that property's type, 'And' it to the filter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="q"></param>
    /// <param name="filterItem"></param>
    /// <returns></returns>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> q, T? filterItem) where T : class
    {
        object? v;
        var predicate = PredicateBuilder.True<T>();

        filterItem?.GetType().GetTypeInfo().GetProperties().ToList()
            .ForEach(delegate (PropertyInfo p)
            {
                v = p.GetValue(filterItem);
                if (v != null && !v.IsDefaultTypeValue())
                {
                    var param = Expression.Parameter(typeof(T), "e");
                    //e => e.Id    
                    var property = Expression.Property(param, p.Name);
                    var value = Expression.Constant(v);
                    //e => e.Id == id
                    var body = Expression.Equal(property, value);
                    var lambda = Expression.Lambda<Func<T, bool>>(body, param);
                    predicate = predicate.And(lambda);
                }
            });

        q = q.Where(predicate);
        return q;
    }

    private static bool IsDefaultTypeValue(this object item)
    {
        return item.Equals(item.GetType().GetDefaultValue());
    }

    private static object? GetDefaultValue(this Type type)
    {
        if (!type.GetTypeInfo().IsValueType)
        {
            return null;
        }

        return typeDefaults.GetOrAdd(type, Activator.CreateInstance(type));
    }
}
