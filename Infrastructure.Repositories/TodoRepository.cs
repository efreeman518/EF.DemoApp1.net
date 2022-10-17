using Package.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Threading;

namespace Infrastructure.Repositories;

public class TodoRepository : RepositoryBase<TodoContext>, ITodoRepository
{
    public TodoRepository(TodoContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// Gets a list of items
    /// </summary>
    /// <param name="pageSize">must be > 0</param>
    /// <param name="pageIndex">1 based index</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<TodoItem>> GetItemsAsync(int pageSize, int pageIndex, CancellationToken cancellationToken = default)
    {
        var error = "";
        if (pageSize < 1) error += "Invalid page size; must be > 0. ";
        if (pageIndex < 1) error += "Invalid page index; must be >= 1.";
        if (error.Length > 0) throw new ValidationException(error);

        var q = DB.TodoItems.AsQueryable().AsNoTracking();
        int skipCount = (pageIndex - 1) * pageSize;
        q = skipCount == 0 ? q.Take(pageSize) : q.Skip(skipCount).Take(pageSize);
        return await q.ToListAsync(cancellationToken);
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
}
