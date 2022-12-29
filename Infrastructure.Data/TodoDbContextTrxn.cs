using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class TodoDbContextTrxn : TodoDbContextBase
{
    public TodoDbContextTrxn(DbContextOptions<TodoDbContextTrxn> options) : base(options)
    {

    }
}
