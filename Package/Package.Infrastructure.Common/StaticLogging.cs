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
    /// Each call re-creates the static logger factory; 
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

    public static ILogger CreateLogger(string categoryName) => _factory.CreateLogger(categoryName);
    public static ILogger<T> CreateLogger<T>() => _factory.CreateLogger<T>();
}
