using Application.Contracts.Model;
using AutoMapper;
using Package.Infrastructure.Data;
using Package.Infrastructure.Data.Contracts;
using System.ComponentModel.DataAnnotations;
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
    /// <exception cref="ValidationException"></exception>
    public async Task<PagedResponse<TodoItemDto>> GetPageTodoItemDtoAsync(int pageSize, int pageIndex, CancellationToken cancellationToken = default)
    {
        (var data, var total) = await DB.Set<TodoItem>().GetPageEntityAsync(pageSize: pageSize, pageIndex: pageIndex, includeTotal: true, cancellationToken: cancellationToken);
        return new PagedResponse<TodoItemDto>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            Data = _mapper.Map<List<TodoItemDto>>(data),
            Total = total
        };
    }
}
