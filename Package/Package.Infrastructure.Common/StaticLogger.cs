using Microsoft.Extensions.Logging;

namespace Package.Infrastructure.Common;

//static class logging
public static class StaticLogger
{
    private static ILoggerFactory LoggerFactory = new LoggerFactory();

    public static void SetLoggerFactory(ILoggerFactory loggerFactory) => LoggerFactory = loggerFactory;
    public static ILogger? CreateLogger<T>() => LoggerFactory?.CreateLogger<T>();
    public static ILogger? CreateLogger(string categoryName) => LoggerFactory?.CreateLogger(categoryName);
}
