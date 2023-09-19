using Application.Contracts.Interfaces;
using Package.Infrastructure.Common;
using Package.Infrastructure.Data;

namespace Infrastructure.Repositories;

public class TodoRepositoryTrxn : RepositoryBase<TodoDbContextTrxn>, ITodoRepositoryTrxn
{
    public TodoRepositoryTrxn(TodoDbContextTrxn dbContext, IRequestContext rc) : base(dbContext, rc)
    {
    }
}
