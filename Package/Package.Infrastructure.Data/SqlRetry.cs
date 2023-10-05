using Microsoft.Data.SqlClient;
using Package.Infrastructure.Common;

namespace Package.Infrastructure.Data;
public static class SqlRetry
{
    //Exception Handling Strategy	Errors Handled
    //https://scottdorman.blog/2020/09/13/database-resiliency-with-polly/
    //SqlServerTransientExceptionHandlingStrategy	40501, 49920, 49919, 49918, 41839, 41325, 41305, 41302, 41301, 40613, 40197, 10936, 10929, 10928, 10060, 10054, 10053, 4221, 4060, 12015, 233, 121, 64, 20
    //SqlServerTransientTransactionExceptionHandlingStrategy	40549, 40550
    //SqlServerTimeoutExceptionHandlingStrategy	-2
    //NetworkConnectivityExceptionHandlingStrategy	11001
    private static readonly int[] _retrySqlErrorNumbers = [40501, 49920, 49919, 49918, 41839, 41325, 41305, 41302, 41301, 40613, 40197, 10936, 10929, 10928, 10060, 10054, 10053, 4221, 4060, 12015, 233, 121, 64, 20, 40549, 40550, -2, 11001];

    //sql - return T
    public static async Task<T> RetrySqlAsync<T>(Func<Task<T>> factory,
        RetrySettings retrySettings, CircuitBreakerSettings circuitBreakerSettings,
        int[]? retrySqlErrorNumbers = null)
    {
        retrySqlErrorNumbers ??= _retrySqlErrorNumbers;
        bool exceptionCallback(Exception ex) => ex is SqlException sqlEx && retrySqlErrorNumbers.Contains(sqlEx.ErrorCode);
        var retryExceptions = new List<Type> { typeof(SqlException) };
        return await PollyRetry.RetryAsync(factory, retrySettings, circuitBreakerSettings, retryExceptions, true, exceptionCallback);
    }

    //sql - no return
    public static async Task RetrySqlNoReturnAsync<T>(Func<Task<T>> factory,
        RetrySettings retrySettings, CircuitBreakerSettings circuitBreakerSettings,
        int[]? retrySqlErrorNumbers = null)
    {
        retrySqlErrorNumbers ??= _retrySqlErrorNumbers;
        bool exceptionCallback(Exception ex) => ex is SqlException sqlEx && retrySqlErrorNumbers.Contains(sqlEx.ErrorCode);
        var retryExceptions = new List<Type> { typeof(SqlException) };
        await PollyRetry.RetryAsync(factory, retrySettings, circuitBreakerSettings, retryExceptions, true, exceptionCallback);
    }
}
