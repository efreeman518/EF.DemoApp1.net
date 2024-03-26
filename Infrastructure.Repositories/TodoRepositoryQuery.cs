using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using AutoMapper;
using Domain.Shared.Enums;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Data;
using Package.Infrastructure.Data.Contracts;

namespace Infrastructure.Repositories;

public class TodoRepositoryQuery(TodoDbContextQuery dbContext, IRequestContext<string> rc, IMapper mapper) : RepositoryBase<TodoDbContextQuery, string>(dbContext, rc), ITodoRepositoryQuery
{
    //compile frequently used query
    //https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-di%2Cexpression-api-with-constant#compiled-queries
    private static readonly Func<TodoDbContextQuery, TodoItemStatus, IAsyncEnumerable<TodoItem>> queryTodoItemsByStatus =
        EF.CompileAsyncQuery((TodoDbContextQuery db, TodoItemStatus status) =>
                   db.TodoItems.Where(t => t.Status == status));

    public IAsyncEnumerable<TodoItem> GetTodoItemsByStatus(TodoItemStatus status)
    {
        return queryTodoItemsByStatus(DB, status);
    }

    /// <summary>
    /// Return a cref="PagedResponse" projected to Dto
    /// </summary>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PagedResponse<TodoItemDto>> SearchTodoItemAsync(SearchRequest<TodoItemSearchFilter> request, CancellationToken cancellationToken = default)
    {
        var q = DB.Set<TodoItem>().ComposeIQueryable(false);

        //further compose IQueryable
        //sorts
        if (request.Sorts != null) q = q.OrderBy(request.Sorts);

        //filter - build the Where clause
        var filter = request.Filter;
        if (filter != null)
        {
            if (filter.Id != null) q = q.Where(e => e.Id == filter.Id);
            if (filter.Name != null)
            {
                q = filter.Name.Contains('*')
                    ? q.Where(e => e.Name.Contains(filter.Name))
                    : q.Where(e => e.Name == filter.Name);
            }
            if (filter.Statuses != null)
            {
                q = q.Where(e => filter.Statuses.Contains(e.Status));
            }
            if (filter.DateStart != null)
            {
                q = q.Where(e => e.CreatedDate >= filter.DateStart);
            }
            if (filter.DateEnd != null)
            {
                q = q.Where(e => e.CreatedDate <= filter.DateEnd);
            }
        }

        //sort and filter have already been applied
        await SetNoLock();
        (var data, var total) = await q.QueryPageProjectionAsync<TodoItem, TodoItemDto>(
            mapper.ConfigurationProvider, pageSize: request.PageSize, pageIndex: request.PageIndex, includeTotal: true, cancellationToken: cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);
        await SetLock();
        return new PagedResponse<TodoItemDto>
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            Data = data, //_mapper.Map<List<TodoItemDto>>(data),
            Total = total
        };
    }
}
