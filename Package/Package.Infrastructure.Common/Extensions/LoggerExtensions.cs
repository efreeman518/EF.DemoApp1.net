using Microsoft.Extensions.Logging;
using Package.Infrastructure.Common.Constants;

namespace Package.Infrastructure.Common.Extensions;

public static class LoggerExtensions
{
    ////Performant Logging
    ////https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage
    //private static readonly Action<ILogger, string, Exception?> _debugLog =
    //    LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, nameof(DebugLog)), "{message}");
    //private static readonly Action<ILogger, string, Exception?> _infoLog =
    //    LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, nameof(InfoLog)), "{message}");
    //private static readonly Action<ILogger, string, Exception?> _errorLog =
    //    LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, nameof(ErrorLog)), "{message}");
    //private static readonly Action<ILogger, string, string, string?, string?, Exception?> _extLog =
    //    LoggerMessage.Define<string, string, string?, string?>(LogLevel.Information, new EventId(1, nameof(ExtLog)),
    //        "{message} {param1} {param2} {param3}");

    //static LoggerExtensions()
    //{
    //}

    //public static void DebugLog(this ILogger logger, string message)
    //{
    //    _debugLog(logger, message, null);
    //}
    //public static void InfoLog(this ILogger logger, string message)
    //{
    //    _infoLog(logger, message, null);
    //}

    //public static void ErrorLog(this ILogger logger, string message, Exception ex)
    //{
    //    _errorLog(logger, message, ex);
    //}

    //public static void ExtLog(this ILogger logger, string message, string param1, string? param2, string? param3, Exception? ex)
    //{
    //    _extLog(logger, message, param1, param2, param3, ex);
    //}

    /// <summary>
    /// Slow logging - dynamic structured logging
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="logLevel"></param>
    /// <param name="eventId"></param>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    /// <param name="logData"></param>
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string? message = null, Exception? exception = null, List<KeyValuePair<string, string?>>? logData = null)
    {
        //structured logging template - ignore src properties if null (background services do not have a request context)
        string logTemplate = "{TimeUTC}{SiteName}{ServerInstanceId}";
        //structured logging value array
        List<object?> logValues =
        [
            DateTime.UtcNow,
            //Azure App Services
            Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "",
            Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? ""
        ];

        if (message != null)
        {
            logTemplate += " {Message}";
            logValues.Add(message);
        }

        //any custom log data items
        logData?.ForEach(a =>
        {
            logTemplate += " {" + a.Key + "}";
            logValues.Add(a.Value);
        });

#pragma warning disable CA2254 // Template should be a static expression

        //structured logging - logTemplate {} items must match the values in logValues
        switch (logLevel)
        {
            case LogLevel.Critical:
                logger.LogCritical(eventId, exception, logTemplate, logValues);

                break;
            case LogLevel.Debug:
                logger.LogDebug(eventId, exception, logTemplate, logValues);
                break;
            case LogLevel.Information:
                logger.LogInformation(eventId, exception, logTemplate, logValues);
                break;
            case LogLevel.Error:
                logger.LogError(eventId, exception, logTemplate, logValues);
                break;
            case LogLevel.Trace:
                logger.LogTrace(eventId, exception, logTemplate, logValues);
                break;
            case LogLevel.Warning:
                logger.LogWarning(eventId, exception, logTemplate, logValues);
                break;
        }

#pragma warning restore CA2254 // Template should be a static expression

    }
}

/// <summary>
/// Source Generated Logging - not currently supported when ILogger is injected by primary constructor 
/// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage
/// </summary>
public static partial class LoggerMessageDefinitionSG
{
    [LoggerMessage(LoggerEventConstants.DebugDefault, LogLevel.Debug, "{message}")]
    public static partial void DebugLog(this ILogger logger, string message);

    [LoggerMessage(LoggerEventConstants.DebugDefault + 1, LogLevel.Debug, "{message} {param1} {param2} {param3}")]
    public static partial void DebugLogExt(this ILogger logger, string message, string? param1 = null, string? param2 = null, string? param3 = null);

    [LoggerMessage(LoggerEventConstants.InfoDefault, LogLevel.Information, "{message}")]
    public static partial void InfoLog(this ILogger logger, string message);

    [LoggerMessage(LoggerEventConstants.InfoDefault + 1, LogLevel.Information, "{message} {param1} {param2} {param3}")]
    public static partial void InfoLogExt(this ILogger logger, string message, string? param1 = null, string? param2 = null, string? param3 = null);

    [LoggerMessage(LoggerEventConstants.ErrorDefault, LogLevel.Error, "{message}", SkipEnabledCheck = true)]
    public static partial void ErrorLog(this ILogger logger, string message, Exception exception);

    [LoggerMessage(LoggerEventConstants.ErrorDefault + 1, LogLevel.Error, "{message} {param1} {param2} {param3}", SkipEnabledCheck = true)]
    public static partial void ErrorLogExt(this ILogger logger, string message, Exception exception, string? param1 = null, string? param2 = null, string? param3 = null);
}