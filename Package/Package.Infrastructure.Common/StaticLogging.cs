using Microsoft.Extensions.Logging;

namespace Package.Infrastructure.Common;

/// <summary>
/// https://stackify.com/net-core-loggerfactory-use-correctly/
/// </summary>
public static class StaticLogging
{
    //single static for the application
    private static ILoggerFactory _factory = new LoggerFactory();

    /// <summary>
    /// Early phase: Create temporary LoggerFactory (before DI is available)
    /// </summary>
    /// <param name="configure">Configure the static application logger factory</param>
    /// <returns></returns>
    public static ILoggerFactory CreateStaticLoggerFactory(Action<ILoggingBuilder>? configure)
    {
        _factory = LoggerFactory.Create(builder =>
        {
            configure?.Invoke(builder);
        });
        return _factory;
    }

    /// <summary>
    /// Later phase:  Set the static logger factory to an externally created factory (e.g., from dependency injection).
    /// </summary>
    public static ILoggerFactory SetStaticLoggerFactory(ILoggerFactory loggerFactory)
    {
        _factory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        return _factory;
    }

    public static ILogger CreateLogger(string categoryName) => _factory.CreateLogger(categoryName);
    public static ILogger<T> CreateLogger<T>() => _factory.CreateLogger<T>();
}
