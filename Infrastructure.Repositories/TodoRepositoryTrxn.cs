using Package.Infrastructure.Common;
using Package.Infrastructure.Data;

namespace Infrastructure.Repositories;

public class TodoRepositoryTrxn : RepositoryBase<TodoDbContextTrxn>, ITodoRepositoryTrxn
{
    public TodoRepositoryTrxn(TodoDbContextTrxn dbContext, IRequestContext src) : base(dbContext, src)
    {
    }
}
