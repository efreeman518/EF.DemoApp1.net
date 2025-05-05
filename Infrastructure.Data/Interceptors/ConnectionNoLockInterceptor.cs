using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace Infrastructure.Data.Interceptors;
public class ConnectionNoLockInterceptor(ILogger<ConnectionNoLockInterceptor> logger) : DbConnectionInterceptor
{
    private readonly ILogger<ConnectionNoLockInterceptor> _logger = logger;

    public override void ConnectionOpened(
        DbConnection connection,
        ConnectionEndEventData eventData)
    {
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED";
            cmd.ExecuteNonQuery();
            _logger.LogInformation("Set READ UNCOMMITTED isolation level for connection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set isolation level");
        }

        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Set READ UNCOMMITTED isolation level for connection (async)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set isolation level");
        }

        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }
}