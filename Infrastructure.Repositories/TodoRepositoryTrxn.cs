using Application.Contracts.Interfaces;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Data;

namespace Infrastructure.Repositories;

public class TodoRepositoryTrxn(TodoDbContextTrxn dbContext, IRequestContext<string, string> rc) : RepositoryBase<TodoDbContextTrxn, string, string>(dbContext, rc), ITodoRepositoryTrxn
{
}
