using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Data;

public class TodoDbContextQuery(DbContextOptions<TodoDbContextQuery> options) : TodoDbContextBase(options)
{
}
