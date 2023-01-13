using Application.Contracts.Model;
using AutoMapper;
using Package.Infrastructure.Data;
using Package.Infrastructure.Data.Contracts;
using System.Threading;

namespace Infrastructure.Repositories;

public class TodoRepositoryQuery : RepositoryBase<TodoDbContextQuery>, ITodoRepositoryQuery
{
    private readonly IMapper _mapper;
    public TodoRepositoryQuery(TodoDbContextQuery dbContext, IAuditDetail audit, IMapper mapper) : base(dbContext, audit)
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
        var q = DB.Set<TodoItem>().ComposePagedIQueryable(false);

        //further compose IQueryable
        //sorts
        if (request.Sorts != null) q = q.OrderBy(request.Sorts);

        //filter - build the Where clause
        var filter = request.Filter;
        if(filter != null)
        {
            if(filter.Id != null) q = q.Where(e => e.Id== filter.Id);
            if(filter.Name != null)
            {
                q = filter.Name.Contains('*')
                    ? q.Where(e => e.Name.Contains(filter.Name))
                    : q.Where(e => e.Name == filter.Name);
            }
            if(filter.Statuses != null)
            {
                q = q.Where(e => filter.Statuses.Contains(e.Status));
            }
            if(filter.DateStart != null)
            {
                q = q.Where(e => e.CreatedDate >= filter.DateStart);
            }
            if (filter.DateEnd != null)
            {
                q = q.Where(e => e.CreatedDate <= filter.DateEnd);
            }
        }

        //sort and filter have already been applied
        (var data, var total) = await q.GetPageEntitiesAsync(pageSize: request.PageSize, pageIndex: request.PageIndex, includeTotal: true, cancellationToken: cancellationToken);
        return new PagedResponse<TodoItemDto>
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            Data = _mapper.Map<List<TodoItemDto>>(data),
            Total = total
        };
    }
}
