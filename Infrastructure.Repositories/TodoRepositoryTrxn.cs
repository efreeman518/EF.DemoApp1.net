using Application.Contracts.Interfaces;
using Package.Infrastructure.Common;
using Package.Infrastructure.Data;

namespace Infrastructure.Repositories;

public class TodoRepositoryTrxn(TodoDbContextTrxn dbContext, IRequestContext rc) : RepositoryBase<TodoDbContextTrxn>(dbContext, rc), ITodoRepositoryTrxn
{
}
