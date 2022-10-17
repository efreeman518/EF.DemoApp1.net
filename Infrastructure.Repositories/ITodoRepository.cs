using Domain.Model;
using Package.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public interface ITodoRepository : IRepositoryBase
{
    Task<List<TodoItem>> GetItemsAsync(int pageSize, int pageIndex, CancellationToken cancellationToken = default);
    Task<int> GetItemsCountAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<TodoItem, bool>> filter, CancellationToken cancellationToken = default);
    Task<TodoItem?> GetItemAsync(Expression<Func<TodoItem, bool>> filter, CancellationToken cancellationToken = default);
    TodoItem AddItem(TodoItem todoItem);
    void UpdateItem(TodoItem todoItem);
    void DeleteItem(TodoItem todoItem);
    void DeleteItem(Guid id);
}
