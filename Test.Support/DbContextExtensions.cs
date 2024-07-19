using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Data.Contracts;

namespace Test.Support;
public static class DbContextExtensions
{
    /// <summary>
    /// Reseed the database with data from the seed files and/or factories specified by the test, and/or from config
    /// InMemoryDatabase has limited functionality; does not support reset, sql execute.
    /// Caller must SaveChangesAsync to persist the seed data
    /// </summary>
    /// <param name="respawn"></param>
    /// <param name="seedFactories"></param>
    /// <param name="seedPaths"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task SeedDatabaseAsync(this DbContext dbContext, ILogger logger, List<string>? seedPaths = null,
        List<Action>? seedFactories = null, CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.IsInMemory()) seedPaths = null;

        //seed
        seedFactories ??= [];
        seedPaths ??= [];

        await dbContext.SeedAsync(logger, [.. seedPaths], "*.sql", [.. seedFactories], cancellationToken);
    }

    /// <summary>
    /// Seed the database with data from the seed files and/or factories specified by the test, and/or from config
    /// Caller or seedFactory delegates must SaveChanges to persist the seedFactories data
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="logger"></param>
    /// <param name="seedPaths"></param>
    /// <param name="searchPattern"></param>
    /// <param name="seedFactories"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task SeedAsync(this DbContext dbContext, ILogger logger, string[]? seedPaths = null,
        string searchPattern = "*.sql", Action[]? seedFactories = null, CancellationToken cancellationToken = default)
    {
        //run seed scripts first since they could affect db structure
        if (!dbContext.Database.IsInMemory() && seedPaths?.Length > 0)
        {
            await dbContext.SeedRawSqlFilesAsync(logger, [.. seedPaths], searchPattern, cancellationToken);
        }

        if (seedFactories != null)
        {
            foreach (var action in seedFactories)
            {
                action();
            }
        }
    }

    /// <summary>
    /// runs the seed sql files in the specified paths; await synchrously to ensure seed files are run in order
    /// </summary>
    /// <param name="db"></param>
    /// <param name="logger"></param>
    /// <param name="relativePaths"></param>
    /// <param name="searchPattern"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task SeedRawSqlFilesAsync(this DbContext db, ILogger logger, List<string> relativePaths, string searchPattern, CancellationToken cancellationToken = default)
    {
        foreach (var path in relativePaths)
        {
            string[] files = [.. Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path), searchPattern).OrderBy(f => f)]; //order by name
            foreach (var filePath in files)
            {
                await db.SeedRawSqlFileAsync(logger, filePath, cancellationToken);
            }
        }
    }

    public static async Task SeedRawSqlFileAsync(this DbContext db, ILogger logger, string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.InfoLog($"Seeding test database from file: {filePath}");
            var sql = await File.ReadAllTextAsync(filePath, cancellationToken);
            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.ErrorLog($"An error occurred seeding the database from file {filePath}", ex);
        }
    }
}
