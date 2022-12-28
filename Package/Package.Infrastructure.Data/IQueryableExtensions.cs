using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Package.Infrastructure.Data.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Package.Infrastructure.Data;
public static class IQueryableExtensions
{
    /// <summary>
    /// Returns the IQueryable for further composition; 
    /// client code expected to subsequently call GetListAsync() with the query to run it async and return results
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="query"></param>
    /// <param name="tracking"></param>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex"></param>
    /// <param name="filter"></param>
    /// <param name="orderBy"></param>
    /// <param name="includes"></param>
    /// <returns></returns>
    public static IQueryable<T> ComposePagedIQueryable<T>(this IQueryable<T> query, bool tracking = false,
        int? pageSize = null, int? pageIndex = null,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Func<IQueryable<T>, IIncludableQueryable<T, object?>>[] includes)
        where T : class
    {
        query = tracking ? query.AsTracking() : query.AsNoTracking();

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
        }

        return query;
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
                new Type[] { source.ElementType, selector.Type },
                expression, Expression.Quote(Expression.Lambda(selector, parameter)));
            count++;
        }
        return count > 0 ? source.Provider.CreateQuery<T>(expression) : source;
    }

    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> q, T? filterItem) where T : class
    {
        //TODO: inspect T for non-default property values and add to the query

        Expression<Func<T, bool>> filter;

        return q;
    }
}
