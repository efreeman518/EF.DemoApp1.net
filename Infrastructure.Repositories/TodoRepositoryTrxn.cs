using Application.Contracts.Interfaces;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Data;

namespace Infrastructure.Repositories;

public class TodoRepositoryTrxn(TodoDbContextTrxn dbContext, IRequestContext<string> rc) : RepositoryBase<TodoDbContextTrxn, string>(dbContext, rc), ITodoRepositoryTrxn
{
}
