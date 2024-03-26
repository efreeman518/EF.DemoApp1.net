using AutoMapper;
using AutoMapper.QueryableExtensions;
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
    /// <param name="mapperConfigProvider"></param>
    /// <param name="tracking"></param>
    /// <param name="filter"></param>
    /// <param name="orderBy"></param>
    /// <param name="splitQuery">Discretionary; avoid cartesian explosion, applicable with Includes; understand the risks/repercussions (when paging, etc) of using this https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries</param>
    /// <param name="includes"></param>
    /// <returns></returns>
    public static IAsyncEnumerable<TProject> GetStreamProjection<T, TProject>(this IQueryable<T> query, IConfigurationProvider mapperConfigProvider,
        bool tracking = false, Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool splitQuery = false,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        return query.ComposeIQueryable(tracking, null, null, filter, orderBy, splitQuery, includes).ProjectTo<TProject>(mapperConfigProvider).AsAsyncEnumerable();
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
