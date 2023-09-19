using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using AutoMapper;
using Package.Infrastructure.Common;
using Package.Infrastructure.Data;
using Package.Infrastructure.Data.Contracts;

namespace Infrastructure.Repositories;

public class TodoRepositoryQuery : RepositoryBase<TodoDbContextQuery>, ITodoRepositoryQuery
{
    private readonly IMapper _mapper;
    public TodoRepositoryQuery(TodoDbContextQuery dbContext, IRequestContext rc, IMapper mapper) : base(dbContext, rc)
    {
        _mapper = mapper;
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
        //await SetNoLock(); //InMemoryDbContext does not support
        (var data, var total) = await q.QueryPageProjectionAsync<TodoItem, TodoItemDto>(
            _mapper.ConfigurationProvider, pageSize: request.PageSize, pageIndex: request.PageIndex, includeTotal: true, cancellationToken: cancellationToken);
        //await SetLock(); //InMemoryDbContext does not support
        return new PagedResponse<TodoItemDto>
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            Data = data, //_mapper.Map<List<TodoItemDto>>(data),
            Total = total
        };
    }
}
