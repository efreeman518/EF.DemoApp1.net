using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Data.Contracts;
using Respawn;
using Respawn.Graph;
using System.Data.Common;
using Testcontainers.MsSql;

namespace Test.Support;

public static class DbTestLifecycle
{
    public static async Task EnsureInitializedAsync(TodoDbContextBase dbContext, bool skipAlwaysEncryptedSetup = false, CancellationToken cancellationToken = default)
    {
        Environment.SetEnvironmentVariable("SKIP_ALWAYS_ENCRYPTED_SETUP", skipAlwaysEncryptedSetup ? "true" : null);

        if (dbContext.Database.IsInMemory())
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public static async Task<(MsSqlContainer Container, string ConnectionString)> StartDbContainerAsync(string dbName,
        string password = "YourStr0ngP@ssword!", bool createDatabase = true, CancellationToken cancellationToken = default)
    {
        var builder = new MsSqlBuilder("mcr.microsoft.com/mssql/server:latest");
        if (!string.IsNullOrWhiteSpace(password))
        {
            builder = builder.WithPassword(password);
        }

        var container = builder.Build();
        await container.StartAsync(cancellationToken);

        string masterConnectionString = container.GetConnectionString();
        if (createDatabase)
        {
            await CreateDatabaseIfNotExistsAsync(masterConnectionString, dbName, cancellationToken);
        }

        string dbConnectionString = masterConnectionString.Replace("master", dbName);
        return (container, dbConnectionString);
    }

    public static async Task<(DbConnection Connection, Respawner Respawner)> OpenRespawnerAsync(string dbConnectionString,
        CancellationToken cancellationToken = default)
    {
        var dbConnection = new SqlConnection(dbConnectionString);
        await dbConnection.OpenAsync(cancellationToken);

        var respawner = await Respawner.CreateAsync(dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["todo"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });

        return (dbConnection, respawner);
    }

    public static async Task ResetDatabaseAsync(TodoDbContextBase dbContext, ILogger logger, string dbConnectionString,
        Respawner? respawner, DbConnection? dbConnection, bool respawn = false, string? dbSnapshotName = null,
        List<string>? seedPaths = null, List<Action>? seedFactories = null, CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsInMemory())
        {
            if (respawn && respawner != null && dbConnection != null)
            {
                await respawner.ResetAsync(dbConnection);
            }

            if (!string.IsNullOrEmpty(dbSnapshotName))
            {
                if (dbConnection == null)
                {
                    throw new InvalidOperationException("Database snapshot restore requires an open database connection.");
                }

                var snapshotUtility = new SqlDatabaseSnapshotUtility(dbConnectionString);
                var dbName = dbConnection.Database;
                await snapshotUtility.RestoreSnapshotAsync(dbName, dbSnapshotName, cancellationToken);
            }
        }

        await dbContext.SeedDatabaseAsync(logger, seedPaths, seedFactories, cancellationToken);
        await dbContext.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, cancellationToken: cancellationToken);
    }

    public static async Task CreateDbSnapshotAsync(string dbSource, string? dbConnectionString, DbConnection? dbConnection,
        string snapshotName, CancellationToken cancellationToken = default)
    {
        EnsureSnapshotsSupported(dbSource, dbConnectionString);

        if (dbConnection == null)
        {
            throw new InvalidOperationException("Database snapshot creation requires an open database connection.");
        }

        await DbSupport.CreateDbSnapshot(snapshotName, dbConnection.Database, dbConnectionString!, cancellationToken);
    }

    public static async Task DeleteDbSnapshotAsync(string dbSource, string? dbConnectionString,
        string snapshotName, CancellationToken cancellationToken = default)
    {
        EnsureSnapshotsSupported(dbSource, dbConnectionString);
        await DbSupport.DeleteDbSnapshot(snapshotName, dbConnectionString!, cancellationToken);
    }

    private static async Task CreateDatabaseIfNotExistsAsync(string masterConnectionString, string dbName,
        CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken);

        using (var command = new SqlCommand($"SELECT DB_ID('{dbName}')", connection))
        {
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result != DBNull.Value)
            {
                return;
            }
        }

        using var createCommand = new SqlCommand($"CREATE DATABASE [{dbName}]", connection);
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void EnsureSnapshotsSupported(string dbSource, string? dbConnectionString)
    {
        string[] notAllowedTypes = ["UseInMemoryDatabase", "TestContainer"];
        if (notAllowedTypes.Contains(dbSource) || string.IsNullOrWhiteSpace(dbConnectionString))
        {
            throw new InvalidOperationException("Snapshots are only allowed for existing SQL DBs");
        }
    }
}