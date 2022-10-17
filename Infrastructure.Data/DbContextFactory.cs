using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace Infrastructure.Data;

/// <summary>
/// Used for design time needing a DbContext (EF Migrations)
/// First run in PMC:
/// $env:EFCORETOOLSDB = "Server=localhost;Database=sample-db;Integrated Security=true;MultipleActiveResultsets=true;Column Encryption Setting=enabled;"
/// if using sql Always Encrypted, set another env variable - the url to the key vault key used as the column master key 
/// - the identity running the PMC commands will need "Key Vault Crypto User" role permissions
/// $env:AKVCMKURL = "https://vault-dev.vault.azure.net/keys/SQL-ColMaskerKey-Default/abc123"
/// EF Core PMC commands
/// https://ef.readthedocs.io/en/staging/miscellaneous/cli/powershell.html#add-migration
/// </summary>
public class DbContextFactory : IDesignTimeDbContextFactory<TodoContext>
{
    public TodoContext CreateDbContext(string[] args)
    {
        string? connString = Environment.GetEnvironmentVariable("EFCORETOOLSDB");

        if (string.IsNullOrEmpty(connString))
            throw new InvalidOperationException("The connection string was not set in the 'EFCORETOOLSDB' environment variable.");

        Console.WriteLine(connString);

        var optionsBuilder = new DbContextOptionsBuilder<TodoContext>();
        optionsBuilder.UseSqlServer(connString);
        return new TodoContext(optionsBuilder.Options);
    }
}
