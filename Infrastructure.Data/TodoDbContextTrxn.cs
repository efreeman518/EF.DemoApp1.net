using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class TodoDbContextTrxn(DbContextOptions<TodoDbContextTrxn> options) : TodoDbContextBase(options)
{
}
