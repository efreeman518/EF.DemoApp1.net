using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Package.Infrastructure.Common.Contracts;
using System.Collections.Concurrent;
using System.Globalization;
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


    #region Conditional Where

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> predicate) =>
        condition ? source.Where(predicate) : source;

    public static IQueryable<T> WhereIfNotNull<T, TValue>(this IQueryable<T> source,
        TValue? value,
        Func<TValue, Expression<Func<T, bool>>> predicateFactory) where TValue : class =>
        value is null ? source : source.Where(predicateFactory(value));

    public static IQueryable<T> WhereIfHasValue<T, TValue>(this IQueryable<T> source,
        TValue? value,
        Func<TValue, Expression<Func<T, bool>>> predicateFactory) where TValue : struct =>
        value.HasValue ? source.Where(predicateFactory(value.Value)) : source;

    public static IQueryable<T> WhereIfNotEmpty<T>(this IQueryable<T> source,
        string? value,
        Func<string, Expression<Func<T, bool>>> predicateFactory) =>
        string.IsNullOrWhiteSpace(value) ? source : source.Where(predicateFactory(value!));

    public static IQueryable<T> WhereIfAny<T, TValue>(this IQueryable<T> source,
        IEnumerable<TValue>? values,
        Func<IEnumerable<TValue>, Expression<Func<T, bool>>> predicateFactory)
    {
        if (values is null) return source;
        var arr = values as TValue[] ?? values.ToArray();
        return arr.Length == 0 ? source : source.Where(predicateFactory(arr));
    }

    #endregion

    #region Predicate pipeline

    /// <summary>
    /// Applies a final composed predicate if it is not null and not trivially 'true'.
    /// </summary>
    public static IQueryable<T> Where<T>(this IQueryable<T> source, Expression<Func<T, bool>>? predicate, bool skipIfTrivialTrue)
    {
        if (predicate == null) return source;
        if (skipIfTrivialTrue && IsTriviallyTrue(predicate))
            return source;
        return source.Where(predicate);
    }

    private static bool IsTriviallyTrue<T>(Expression<Func<T, bool>> expr)
    {
        return expr.Body.NodeType == ExpressionType.Constant &&
               expr.Body is ConstantExpression c &&
               c.Value is bool b && b;
    }

    #endregion

    #region Dynamic (multi) ordering

    /// <summary>
    /// Sort specification (Property path + direction).
    /// </summary>
    public readonly record struct SortSpec(string PropertyPath, bool Descending = false)
    {
        public override string ToString() => Descending ? "-" + PropertyPath : PropertyPath;
    }

    /// <summary>
    /// Order by a single property path (e.g. "Person.LastName"), ascending by default.
    /// </summary>
    public static IOrderedQueryable<T> OrderByProperty<T>(this IQueryable<T> source, string propertyPath, bool descending = false)
    {
        var param = Expression.Parameter(typeof(T), "x");
        Expression body = param;
        foreach (var part in propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            body = Expression.PropertyOrField(body, part);
        }

        var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), body.Type);
        var lambda = Expression.Lambda(delegateType, body, param);

        var methodName = descending ? "OrderByDescending" : "OrderBy";
        var method = (from m in typeof(Queryable).GetMethods()
                      where m.Name == methodName
                      let parms = m.GetParameters()
                      where parms.Length == 2
                      select m).Single().MakeGenericMethod(typeof(T), body.Type);

        return (IOrderedQueryable<T>)method.Invoke(null, [source, lambda])!;
    }

    /// <summary>
    /// ThenBy variant for previously ordered query.
    /// </summary>
    public static IOrderedQueryable<T> ThenByProperty<T>(this IOrderedQueryable<T> source, string propertyPath, bool descending = false)
    {
        var param = Expression.Parameter(typeof(T), "x");
        Expression body = param;
        foreach (var part in propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            body = Expression.PropertyOrField(body, part);
        }

        var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), body.Type);
        var lambda = Expression.Lambda(delegateType, body, param);

        var methodName = descending ? "ThenByDescending" : "ThenBy";
        var method = (from m in typeof(Queryable).GetMethods()
                      where m.Name == methodName
                      let parms = m.GetParameters()
                      where parms.Length == 2
                      select m).Single().MakeGenericMethod(typeof(T), body.Type);

        return (IOrderedQueryable<T>)method.Invoke(null, [source, lambda])!;
    }

    /// <summary>
    /// Applies multiple sorts in order. Accepts either SortSpec or parseable strings.
    /// String formats supported: "Name", "-Name", "Name DESC", "Name ASC".
    /// </summary>
    public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, IEnumerable<string> sortExpressions)
    {
        if (sortExpressions is null) return source;
        SortSpec[] specs = sortExpressions
            .Select(ParseSortExpression)
            .Where(s => !string.IsNullOrWhiteSpace(s.PropertyPath))
            .ToArray();

        return source.OrderBy(specs);
    }

    /// <summary>
    /// Applies multiple sorts using SortSpec collection.
    /// </summary>
    public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, IEnumerable<SortSpec>? specs)
    {
        if (specs == null) return source;
        var specArray = specs as SortSpec[] ?? specs.ToArray();
        if (specArray.Length == 0) return source;

        IOrderedQueryable<T>? ordered = null;
        for (int i = 0; i < specArray.Length; i++)
        {
            var spec = specArray[i];
            ordered = i == 0
                ? source.OrderByProperty(spec.PropertyPath, spec.Descending)
                : ordered!.ThenByProperty(spec.PropertyPath, spec.Descending);
        }

        return ordered ?? source;
    }

    private static SortSpec ParseSortExpression(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return default;
        raw = raw.Trim();
        bool descending = false;

        // Leading '-' shorthand
        if (raw.StartsWith("-", StringComparison.Ordinal))
        {
            descending = true;
            raw = raw[1..].Trim();
        }
        else
        {
            // Trailing direction tokens
            var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                raw = parts[0];
                var dir = parts[1];
                if (dir.Equals("DESC", StringComparison.OrdinalIgnoreCase) ||
                    dir.Equals("DESCENDING", StringComparison.OrdinalIgnoreCase))
                    descending = true;
            }
        }

        return new SortSpec(raw, descending);
    }

    #endregion

    #region Paging helper (Apply after ordering)

    /// <summary>
    /// Applies Skip/Take only if pageSize &gt; 0.
    /// </summary>
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> source, int pageIndex, int pageSize)
    {
        if (pageSize <= 0) return source;
        if (pageIndex < 0) pageIndex = 0;
        return source.Skip(pageIndex * pageSize).Take(pageSize);
    }

    #endregion

    #region Misc helpers

    /// <summary>
    /// Applies a dynamic list of equality filters: (T x) =&gt; x.Property == value (skips nulls).
    /// propertyValuePairs: propertyPath -> value.
    /// </summary>
    public static IQueryable<T> WhereEquals<T>(this IQueryable<T> source, IReadOnlyDictionary<string, object?> propertyValuePairs)
    {
        if (propertyValuePairs.Count == 0) return source;
        var param = Expression.Parameter(typeof(T), "x");
        Expression? combined = null;

        foreach (var kvp in propertyValuePairs)
        {
            if (kvp.Value is null) continue;
            Expression body = param;
            foreach (var part in kvp.Key.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                body = Expression.PropertyOrField(body, part);
            }

            var constant = Expression.Constant(kvp.Value);
            Expression comparison;

            if (kvp.Value.GetType() != body.Type)
            {
                // attempt conversion (e.g., boxed int to Nullable<int>)
                var converted = ConvertValue(kvp.Value, body.Type);
                comparison = Expression.Equal(body, Expression.Constant(converted, body.Type));
            }
            else
            {
                comparison = Expression.Equal(body, constant);
            }

            combined = combined == null ? comparison : Expression.AndAlso(combined, comparison);
        }

        if (combined == null) return source;

        var lambda = Expression.Lambda<Func<T, bool>>(combined, param);
        return source.Where(lambda);
    }

    private static object? ConvertValue(object value, Type targetType)
    {
        if (targetType.IsInstanceOfType(value)) return value;

        var underlying = Nullable.GetUnderlyingType(targetType);
        var destType = underlying ?? targetType;

        return System.Convert.ChangeType(value, destType, CultureInfo.InvariantCulture);
    }

    #endregion
}
