using Domain.Model;
using Microsoft.EntityFrameworkCore;
using Package.Infrastructure.Data;
using Package.Infrastructure.Data.Contracts;
using System.Linq.Expressions;

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

public abstract class TodoDbContextBase(DbContextOptions options) : DbContextBase<string, Guid?>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // schema
        modelBuilder.HasDefaultSchema("todo");

        // table configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TodoDbContextBase).Assembly);

        // datatype defaults for sql
        ConfigureDefaultDataTypes(modelBuilder);

        // Use class name as table name
        SetTableNames(modelBuilder);

        // Configure tenant query filters
        ConfigureTenantQueryFilters(modelBuilder);
    }

    //runs only once per context so when pooled, essentially each DbContext is long-lived and re-used, OnConfiguring not run for each new 'scoped' use
    //override protected void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    //optionsBuilder.AddInterceptors(auditInterceptor);
    //    base.OnConfiguring(optionsBuilder);
    //}

    private static void SetTableNames(ModelBuilder modelBuilder)
    {
        // Use class name as table name - no pluralization
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.DisplayName());
        }
    }

    private static void ConfigureDefaultDataTypes(ModelBuilder modelBuilder)
    {
        // decimal
        var decimalProperties = modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
            .Where(p => p.GetColumnType() == null);

        foreach (var property in decimalProperties)
        {
            property.SetColumnType("decimal(10,4)");
        }

        // dates
        var dateProperties = modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
            .Where(p => p.GetColumnType() == null);

        foreach (var property in dateProperties)
        {
            property.SetColumnType("datetime2");
        }
    }

    private void ConfigureTenantQueryFilters(ModelBuilder modelBuilder)
    {
        var tenantEntityClrTypes = modelBuilder.Model.GetEntityTypes()
            .Where(entityType => typeof(ITenantEntity<Guid>).IsAssignableFrom(entityType.ClrType))
            .Select(entityType => entityType.ClrType);

        var effectiveTenantId = TenantId ?? Guid.Empty;

        foreach (var clrType in tenantEntityClrTypes)
        {
            var filter = BuildTenantFilter(clrType, effectiveTenantId);
            modelBuilder.Entity(clrType).HasQueryFilter(filter);
        }
    }


    //DbSets
    public DbSet<TodoItem> TodoItems { get; set; } = null!;
    public DbSet<SystemSetting> SystemSettings { get; set; } = null!;
    //public DbSet<AuditEntry> AuditEntries { get; set; } = null!;
}
