using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class TodoDbContextQuery : TodoDbContextBase
{
    public TodoDbContextQuery(DbContextOptions<TodoDbContextQuery> options) : base(options)
    {

    }
}
