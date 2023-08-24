using Domain.Model;
using Microsoft.EntityFrameworkCore;
using Package.Infrastructure.Data;

namespace Infrastructure.Data;

public class SystemSettingsDbContext : DbContextBase
{

    public SystemSettingsDbContext(DbContextOptions<SystemSettingsDbContext> options) : base(options)
    {

    }

    //DbSets
    public DbSet<SystemSetting> SystemSettings { get; set; } = null!;
}
