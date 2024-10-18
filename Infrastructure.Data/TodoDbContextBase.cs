using Domain.Model;
using Microsoft.EntityFrameworkCore;
using Package.Infrastructure.Data;

namespace Infrastructure.Data;

//EF Core PMC commands
//https://ef.readthedocs.io/en/staging/miscellaneous/cli/powershell.html#add-migration

//Null reference
//https://docs.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types

//Fluent Migration
//http://www.learnentityframeworkcore.com/configuration/fluent-api/totable-method

/*
 * Migrations applied during design time use the design time factory to create a DBContext; 
 * set the env variable for connection string in PMC before running commands
 * $env:EFCORETOOLSDB = "Server=[server name];Database=[db name];Integrated Security=true;MultipleActiveResultSets=True;Column Encryption Setting=enabled;TrustServerCertificate=true"
 * 
 * if using Always Encrypted column encryption with Azure Key Vault, set env var path to the AKV key used for SQL Column Master Key
 * $env:AKVCMKURL = "https://vault-dev.vault.azure.net/keys/SQL-ColMaskerKey-Default/abc123"
 * 
 * Note - EF migrations design time will run the app to build the service collection, 
 * then fail out - this is ok, PMC will show Microsoft.Extensions.Hosting.HostAbortedException: The host was aborted.
 * 
 * if EF6 and EFCore tools both installed, prefix with EntityFrameworkCore\
 * EntityFrameworkCore\update-database -migration 0  -Context TodoDbContextTrxn : Db back to ground zero
 * EntityFrameworkCore\remove-migration -Context TodoDbContextTrxn : removes the last migration
 * EntityFrameworkCore\add-migration [name] -Context TodoDbContextTrxn : adds a new migration
 * EntityFrameworkCore\script-migration --idempotent -Context TodoDbContextTrxn // -From [starting migration] -To [migration]
 * EntityFrameworkCore\update-database
 * 
 * generate a migration-bundle .exe to apply migrations in a pipeline https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli
 * EntityFrameworkCore\Bundle-Migration -SelfContained -TargetRuntime linux-x64 -Context TodoDbContextTrxn -output ./migrations-exe
 * then run the exe in the pipeline: .\efbundle.exe --connection '[connection string]'
 */

/* Default Retry execution strategy when adding context to DI (in Bootstrapper) does not support user initiated transactions:
 * https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency
 */

/* Enable multi-tenant (with DbContext pooling) 
 * https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy
 * https://learn.microsoft.com/en-us/ef/core/querying/filters
 * https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-di%2Cexpression-api-with-constant
 */

public abstract class TodoDbContextBase(DbContextOptions options) : DbContextBase(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //schema
        modelBuilder.HasDefaultSchema("todo");

        //datatype defaults for sql
        //decimal
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            if (property.GetColumnType() == null) property.SetColumnType("decimal(10,4)");
        }
        //dates
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
        {
            if (property.GetColumnType() == null) property.SetColumnType("datetime2");
        }

        //table configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TodoDbContextBase).Assembly);
    }

    override protected void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    //DbSets
    public DbSet<TodoItem> TodoItems { get; set; } = null!;
    public DbSet<SystemSetting> SystemSettings { get; set; } = null!;
    //public DbSet<AuditEntry> AuditEntries { get; set; } = null!;
}
