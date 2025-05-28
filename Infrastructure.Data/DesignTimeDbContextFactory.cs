﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Data;

[ExcludeFromCodeCoverage]
/// <summary>
/// Used for design time needing a DbContext (EF Migrations)
/// First run in PMC:
/// $env:EFCORETOOLSDB = "Server=[server name];Database=[db name];Integrated Security=true;MultipleActiveResultsets=true;Column Encryption Setting=enabled;TrustServerCertificate=true"
/// if using sql Always Encrypted, set another env variable - the url to the key vault key used as the column master key 
/// - the identity running the PMC commands will need "Key Vault Crypto User" role permissions
/// $env:AKVCMKURL = "https://vault-dev.vault.azure.net/keys/SQL-ColMaskerKey-Default/abc123"
/// EF Core PMC commands
/// https://ef.readthedocs.io/en/staging/miscellaneous/cli/powershell.html#add-migration
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TodoDbContextTrxn>
{
    public TodoDbContextTrxn CreateDbContext(string[] args)
    {
        //Attach debugger?
        //System.Diagnostics.Debugger.Launch();

        string? connString = Environment.GetEnvironmentVariable("EFCORETOOLSDB");

        if (string.IsNullOrEmpty(connString))
            throw new InvalidOperationException("The connection string was not set in the 'EFCORETOOLSDB' environment variable.");

        Console.WriteLine(connString);

        var optionsBuilder = new DbContextOptionsBuilder<TodoDbContextTrxn>();
        optionsBuilder.UseSqlServer(connString);
        return new TodoDbContextTrxn(optionsBuilder.Options) { AuditId = "DesignTimeAuditId", TenantId = Guid.NewGuid() };
    }
}
