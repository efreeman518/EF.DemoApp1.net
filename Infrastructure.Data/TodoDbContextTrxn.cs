using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class TodoDbContextTrxn : TodoDbContextBase
{
    public TodoDbContextTrxn(DbContextOptions options) : base(options)
    {

    }
}
