using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Package.Infrastructure.AspNetCore;

public static class ProblemDetailsHelper
{
    public static ProblemDetails BuildProblemDetailsResponse(string? title = "Error", string? message = null, Exception? exception = null)
    {
        var statusCode = exception?.GetType().Name switch
        {
            "ValidationException" => StatusCodes.Status400BadRequest,
            "NotFoundException" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Type = exception?.GetType().Name,
            Title = title,
            Detail = exception?.Message ?? message,
            Status = statusCode
        };

        //problemDetails.Extensions.Add("traceidentifier", httpContext.TraceIdentifier);
        //if (hostEnvironment.IsDevelopment())
        //{
        //    problemDetails.Extensions.Add("stacktrace", exception.StackTrace);
        //}

        return problemDetails;
    }
}
