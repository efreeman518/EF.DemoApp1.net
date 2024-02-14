using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Package.Infrastructure.AspNetCore;

/// <summary>
/// Add functionality to the host
/// </summary>
public static class IHostEnvironmentExtensions
{
    /// <summary>
    /// Build a ProblemDetails response; unhandled exceptions will be handled by the DefaultExceptionHandler, 
    /// however when controlling flow using Result<T> instead of throwing exceptions all the way out, 
    /// this method can be used to build a ProblemDetails response
    /// </summary>
    /// <param name="title"></param>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    /// <param name="traceId"></param>
    /// <returns></returns>
    public static ObjectResult BuildProblemDetailsResponse(this IHostEnvironment env, string? title = "Error", string? message = null, Exception? exception = null, string? traceId = null)
    {
        //map exception to status code
        var statusCode = exception?.GetType().Name switch
        {
            "ValidationException" => StatusCodes.Status400BadRequest,
            "NotFoundException" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Type = exception?.GetType().Name ?? "Error",
            Title = title ?? "Error occurred",
            Detail = message ?? exception?.Message,
            Status = statusCode
        };
        if (traceId != null) problemDetails.Extensions.Add("traceId", traceId);
        problemDetails.Extensions.Add("machineName", Environment.MachineName);

        if (env.IsDevelopment())
        {
            problemDetails.Extensions.Add("stacktrace", exception?.StackTrace);
        }

        ObjectResult result = new(problemDetails)
        {
            StatusCode = statusCode
        };

        return result;
    }
}
