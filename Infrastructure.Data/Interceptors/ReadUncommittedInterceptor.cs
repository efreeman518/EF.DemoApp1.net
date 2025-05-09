using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace Infrastructure.Data.Interceptors;
public class ReadUncommittedInterceptor(ILogger<ReadUncommittedInterceptor> logger) : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        if (!command.CommandText.StartsWith("SET TRANSACTION ISOLATION LEVEL"))
        {
            var originalCommand = command.CommandText;
            command.CommandText = "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; " + command.CommandText;

            logger.LogDebug("Modified command isolation level. Original: {OriginalCommand}", originalCommand);
        }

        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (!command.CommandText.StartsWith("SET TRANSACTION ISOLATION LEVEL"))
        {
            var originalCommand = command.CommandText;
            command.CommandText = "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; " + command.CommandText;

            logger.LogDebug("Modified command isolation level (async). Original: {OriginalCommand}", originalCommand);
        }

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
