using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Common.Extensions;

namespace Test.Support;
public static class DbContextExtensions
{
    public static async Task Seed(this DbContext dbContext, ILogger logger, string[]? seedPaths = null,
        string searchPattern = "*.sql", Action[]? seedFactories = null, CancellationToken cancellationToken = default)
    {
        if (seedPaths?.Length > 0)
        {
            await dbContext.SeedRawSqlFilesAsync(logger, [.. seedPaths], searchPattern, cancellationToken);
        }

        if (seedFactories != null)
        {
            foreach (var action in seedFactories)
            {
                action();
                await dbContext.SaveChangesAsync(cancellationToken);
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
            var sql = File.ReadAllText(filePath);
            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.ErrorLog($"An error occurred seeding the database from file {filePath}", ex);
        }
    }
}
