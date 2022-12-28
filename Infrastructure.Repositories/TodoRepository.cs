using Application.Contracts.Model;
using AutoMapper;
using Package.Infrastructure.Data;
using Package.Infrastructure.Data.Contracts;
using System.ComponentModel.DataAnnotations;
using System.Threading;

namespace Infrastructure.Repositories;

public class TodoRepository : RepositoryTBase<TodoContext>, ITodoRepository
{
    private readonly IMapper _mapper;
    public TodoRepository(TodoContext dbContext, IMapper mapper, IAuditDetail audit) : base(dbContext, audit)
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
    public async Task<PagedResponse<TodoItemDto>> GetDtosPagedAsync(int pageSize, int pageIndex, CancellationToken cancellationToken = default)
    {
        (var data, var total) = await DB.Set<TodoItem>().GetPagedListAsync(pageSize: pageSize, pageIndex: pageIndex, includeTotal: true, cancellationToken: cancellationToken);
        return new PagedResponse<TodoItemDto>
        {
            PageIndex = pageIndex,
            PageSize= pageSize,
            Data = _mapper.Map<List<TodoItemDto>>(data),
            Total = total
        };
    }
}
