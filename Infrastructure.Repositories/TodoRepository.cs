using Application.Contracts.Model;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Package.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Threading;

namespace Infrastructure.Repositories;

public class TodoRepository : RepositoryBase<TodoContext>, ITodoRepository
{
    private readonly IMapper _mapper;
    public TodoRepository(TodoContext dbContext, IMapper mapper) : base(dbContext)
    {
        _mapper = mapper;
    }

    /// <summary>
    /// Gets a list of items
    /// </summary>
    /// <param name="pageSize">must be > 0</param>
    /// <param name="pageIndex">1 based index</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<TodoItem>> GetItemsPagedAsync(int pageSize, int pageIndex, CancellationToken cancellationToken = default)
    {
        return await BuildIQueryablePage(pageSize, pageIndex).ToListAsync(cancellationToken);
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
        (var q, var total) = await BuildIQueryablePageWithTotal(pageSize, pageIndex);
        return new PagedResponse<TodoItemDto>
        {
            PageIndex = pageIndex,
            PageSize= pageSize,
            Data = await q.ProjectTo<TodoItemDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken: cancellationToken),
            Total = total
        };
    }

    public async Task<int> GetItemsCountAsync(CancellationToken cancellationToken = default)
    {
        return await DB.TodoItems.CountAsync(cancellationToken);
    }

    public async Task<TodoItem?> GetItemAsync(Expression<Func<TodoItem, bool>> filter, CancellationToken cancellationToken = default)
    {
        return await DB.TodoItems.FirstOrDefaultAsync(filter, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Expression<Func<TodoItem, bool>> filter, CancellationToken cancellationToken = default)
    {
        return await DB.TodoItems.AnyAsync(filter, cancellationToken);
    }

    public TodoItem AddItem(TodoItem todoItem)
    {
        return DB.TodoItems.Add(todoItem).Entity;
    }

    public void UpdateItem(TodoItem todoItem)
    {
        DB.Update(todoItem); //sets State = Modified, full record wil be updated
    }

    /// <summary>
    /// Using the entity, return if already attached, 
    /// otherwise find in local DbSet<T> if exists (and Attach if necessary), otherwise Attach ref entity
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dbContext"></param>
    /// <param name="entity"></param>
    private void GetLocalOrAttach(ref TodoItem entity)
    {
        //already attached 
        if (DB.Entry(entity).State != EntityState.Detached) return;

        bool attach;
        Guid id = entity.Id;
        TodoItem? localEntity = DB.Set<TodoItem>().Local.FirstOrDefault(e => e.Id == id);
        if (localEntity != null)
        {
            entity = localEntity;
            attach = DB.Entry(entity).State == EntityState.Detached;
        }
        else
        {
            attach = true;
        }
        if (attach) DB.Attach(entity); //sets State = Unchanged
    }

    public void DeleteItem(TodoItem todoItem)
    {
        GetLocalOrAttach(ref todoItem);
        DB.TodoItems.Remove(todoItem);
    }

    public void DeleteItem(Guid id)
    {
        DeleteItem(new TodoItem { Id = id });
    }

    private IQueryable<TodoItem> BuildIQueryableForPaging(int pageSize, int pageIndex)
    {
        var error = "";
        if (pageSize < 1) error += "Invalid page size; must be > 0. ";
        if (pageIndex < 1) error += "Invalid page index; must be >= 1.";
        if (error.Length > 0) throw new ValidationException(error);

        return DB.TodoItems.AsQueryable().AsNoTracking();
    }

    private IQueryable<TodoItem> BuildIQueryablePage(int pageSize, int pageIndex)
    {
        var q = BuildIQueryableForPaging(pageSize, pageIndex);
        int skipCount = (pageIndex - 1) * pageSize;
        return skipCount == 0 ? q.Take(pageSize) : q.Skip(skipCount).Take(pageSize);
    }

    private async Task<(IQueryable<TodoItem>, int)> BuildIQueryablePageWithTotal(int pageSize, int pageIndex)
    {
        var q = BuildIQueryableForPaging(pageSize, pageIndex);
        var total = await q.CountAsync();
        int skipCount = (pageIndex - 1) * pageSize;
        return (skipCount == 0 ? q.Take(pageSize) : q.Skip(skipCount).Take(pageSize), total);
    }
}
