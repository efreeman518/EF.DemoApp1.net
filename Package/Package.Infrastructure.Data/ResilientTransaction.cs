using Microsoft.EntityFrameworkCore;

namespace Package.Infrastructure.Data;

/// <summary>
/// https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-resilient-entity-framework-core-sql-connections
/// 
/// </summary>
public class ResilientTransaction
{
    private readonly DbContext _context;
    private ResilientTransaction(DbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public static ResilientTransaction New(DbContext context) => new(context);

    public async Task ExecuteAsync(Func<Task> action)
    {
        // Use of an EF Core resiliency strategy when using multiple DbContexts
        // within an explicit BeginTransaction():
        // https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(ConfigureAwaitOptions.None);
            await action().ConfigureAwait(ConfigureAwaitOptions.None);
            await transaction.CommitAsync().ConfigureAwait(ConfigureAwaitOptions.None);
        }).ConfigureAwait(ConfigureAwaitOptions.None);
    }
}
