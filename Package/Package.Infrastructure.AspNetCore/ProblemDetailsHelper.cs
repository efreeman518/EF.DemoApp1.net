using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Package.Infrastructure.AspNetCore;

/// <summary>
/// Build a ProblemDetails response
/// </summary>
public static class ProblemDetailsHelper
{
    /// <summary>
    /// Optional global resolver (can be set once at startup) used when no per-call resolver is supplied.
    /// Return null from the resolver to fall back to default mapping.
    /// </summary>
    public static Func<Exception?, int?>? GlobalStatusCodeResolver { get; set; }

    /// <summary>
    /// Default internal mapping used only if:
    /// 1) statusCodeOverride is null AND
    /// 2) per-call resolver returns null or not provided AND
    /// 3) GlobalStatusCodeResolver returns null or not provided.
    /// </summary>
    private static int DefaultStatusCodeMapping(Exception? ex) =>
        ex?.GetType().Name switch
        {
            "ValidationException" => StatusCodes.Status400BadRequest,
            "InvalidOperationException" => StatusCodes.Status400BadRequest,
            "NotFoundException" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

    /// <summary>
    /// Build a ProblemDetails. If providing a custom status code mapping, pass a resolver delegate.
    /// Resolution order:
    /// 1) statusCodeOverride (if provided)
    /// 2) statusCodeResolver(exception) (if provided and returns non-null)
    /// 3) GlobalStatusCodeResolver(exception) (if set and returns non-null)
    /// 4) DefaultStatusCodeMapping
    /// </summary>
    public static ProblemDetails BuildProblemDetailsResponse(
        string title = "Error",
        string? message = null,
        Exception? exception = null,
        string? traceId = null,
        bool includeStackTrace = false,
        int? statusCodeOverride = null,
        Func<Exception?, int?>? statusCodeResolver = null)
    {
        var resolved = statusCodeOverride
            ?? statusCodeResolver?.Invoke(exception)
            ?? GlobalStatusCodeResolver?.Invoke(exception)
            ?? DefaultStatusCodeMapping(exception);

        var problemDetails = new ProblemDetails
        {
            Type = exception?.GetType().Name ?? "Error",
            Title = title,
            Detail = message ?? exception?.Message,
            Status = resolved
        };

        problemDetails.Extensions.TryAdd("traceId", traceId);
        problemDetails.Extensions.TryAdd("machineName", Environment.MachineName);

        if (includeStackTrace && exception?.StackTrace != null)
        {
            problemDetails.Extensions.TryAdd("stacktrace", exception.StackTrace);
        }

        return problemDetails;
    }

    /// <summary>
    /// Build a ProblemDetails with multiple messages combined.
    /// Accepts same resolver pattern as the single-message overload.
    /// </summary>
    public static ProblemDetails BuildProblemDetailsResponseMultiple(
        string title = "Error",
        IReadOnlyList<string>? messages = null,
        string messageDelimiter = ",",
        Exception? exception = null,
        string? traceId = null,
        bool includeStackTrace = false,
        int? statusCodeOverride = null,
        Func<Exception?, int?>? statusCodeResolver = null)
    {
        return BuildProblemDetailsResponse(
            title: title,
            message: messages is { Count: > 0 } ? string.Join(messageDelimiter, messages) : null,
            exception: exception,
            traceId: traceId,
            includeStackTrace: includeStackTrace,
            statusCodeOverride: statusCodeOverride,
            statusCodeResolver: statusCodeResolver);
    }
}
