using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class TodoDbContextQuery : TodoDbContextBase
{
    public TodoDbContextQuery(DbContextOptions options)  : base(options)
    {

    }
}
