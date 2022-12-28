using Package.Infrastructure.Data;
using Package.Infrastructure.Data.Contracts;

namespace Infrastructure.Repositories;

public class TodoRepositoryTrxn : RepositoryBase<TodoDbContextTrxn>, ITodoRepositoryTrxn
{
    public TodoRepositoryTrxn(TodoDbContextTrxn dbContext, IAuditDetail audit) : base(dbContext, audit)
    {
    }
}
