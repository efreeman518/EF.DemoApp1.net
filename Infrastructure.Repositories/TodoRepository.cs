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
        var q = DB.Set<TodoItem>().ComposePagedIQueryable(pageSize: pageSize, pageIndex:pageIndex);
        (var data, var total) = await DB.Set<TodoItem>().GetPagedListAsync(pageSize: pageSize, pageIndex: pageIndex, cancellationToken: cancellationToken);
        return new PagedResponse<TodoItemDto>
        {
            PageIndex = pageIndex,
            PageSize= pageSize,
            //Data = await q.ProjectTo<TodoItemDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken: cancellationToken),
            Data = _mapper.Map<List<TodoItemDto>>(data),
            Total = total
        };
    }

    //public async Task<int> GetItemsCountAsync(CancellationToken cancellationToken = default)
    //{
    //    return await DB.TodoItems.CountAsync(cancellationToken);
    //}

    //public async Task<TodoItem?> GetItemAsync(Expression<Func<TodoItem, bool>> filter, CancellationToken cancellationToken = default)
    //{
    //    return await DB.TodoItems.FirstOrDefaultAsync(filter, cancellationToken);
    //}

    //public async Task<bool> ExistsAsync(Expression<Func<TodoItem, bool>> filter, CancellationToken cancellationToken = default)
    //{
    //    return await DB.TodoItems.AnyAsync(filter, cancellationToken);
    //}

    //public TodoItem AddItem(TodoItem todoItem)
    //{
    //    return DB.TodoItems.Add(todoItem).Entity;
    //}

    //public void UpdateItem(TodoItem todoItem)
    //{
    //    DB.Update(todoItem); //sets State = Modified, full record wil be updated
    //}

    //public void DeleteItem(TodoItem todoItem)
    //{
    //    DB.GetLocalOrAttach(ref todoItem);
    //    DB.TodoItems.Remove(todoItem);
    //}

    //public void DeleteItem(Guid id)
    //{
    //    DeleteItem(new TodoItem { Id = id });
    //}

    //private IQueryable<TodoItem> BuildIQueryableForPaging(int pageSize, int pageIndex)
    //{
    //    var error = "";
    //    if (pageSize < 1) error += "Invalid page size; must be > 0. ";
    //    if (pageIndex < 1) error += "Invalid page index; must be >= 1.";
    //    if (error.Length > 0) throw new ValidationException(error);

    //    return DB.TodoItems.AsQueryable().AsNoTracking();
    //}

    //private IQueryable<TodoItem> BuildIQueryablePage(int pageSize, int pageIndex)
    //{
    //    var q = BuildIQueryableForPaging(pageSize, pageIndex);
    //    int skipCount = (pageIndex - 1) * pageSize;
    //    return skipCount == 0 ? q.Take(pageSize) : q.Skip(skipCount).Take(pageSize);
    //}

    //private async Task<(IQueryable<TodoItem>, int)> BuildIQueryablePageWithTotal(int pageSize, int pageIndex)
    //{
    //    var q = BuildIQueryableForPaging(pageSize, pageIndex);
    //    var total = await q.CountAsync();
    //    int skipCount = (pageIndex - 1) * pageSize;
    //    return (skipCount == 0 ? q.Take(pageSize) : q.Skip(skipCount).Take(pageSize), total);
    //}
}
