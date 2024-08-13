using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Package.Infrastructure.AspNetCore;

/// <summary>
/// Build a ProblemDetails response
/// </summary>
public static class ProblemDetailsHelper
{
    /// <summary>
    /// Build a ProblemDetails response; unhandled exceptions will be handled by the DefaultExceptionHandler, 
    /// however when controlling flow using Result<T> instead of throwing exceptions all the way out, 
    /// this method returns a TypedResult with ProblemDetails response
    /// </summary>
    /// <param name="title"></param>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    /// <param name="traceId"></param>
    /// <param name="includeStackTrace"></param>
    /// <param name="statusCodeOverride"></param>
    /// <returns></returns>
    public static IResult BuildProblemDetailsResponse(string title = "Error", string? message = null, Exception? exception = null, string? traceId = null, bool includeStackTrace = false, int? statusCodeOverride = null)
    {
        //map exception to status code
        var statusCode = statusCodeOverride ?? exception?.GetType().Name switch
        {
            "ValidationException" => StatusCodes.Status400BadRequest,
            "InvalidOperationException" => StatusCodes.Status400BadRequest,
            "NotFoundException" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Type = exception?.GetType().Name ?? "Error",
            Title = title,
            Detail = message ?? exception?.Message,
            Status = statusCode
        };
        if (traceId != null) problemDetails.Extensions.Add("traceId", traceId);
        problemDetails.Extensions.Add("machineName", Environment.MachineName);

        if (includeStackTrace && exception?.StackTrace != null)
        {
            problemDetails.Extensions.Add("stacktrace", exception.StackTrace);
        }

        var result = TypedResults.Problem(problemDetails);

        return result;
    }
}
