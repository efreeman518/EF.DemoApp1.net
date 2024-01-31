using Application.Contracts.Interfaces;
using Package.Infrastructure.Common;
using Package.Infrastructure.Data;

namespace Infrastructure.Repositories;

public class TodoRepositoryTrxn(TodoDbContextTrxn dbContext, IRequestContext<string> rc) : RepositoryBase<TodoDbContextTrxn, string>(dbContext, rc), ITodoRepositoryTrxn
{
}
