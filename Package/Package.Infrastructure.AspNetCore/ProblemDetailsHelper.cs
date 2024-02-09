using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Package.Infrastructure.AspNetCore;

public static class ProblemDetailsHelper
{
    public static ProblemDetails BuildProblemDetailsResponse(string? message = null, Exception? exception = null)
    {
        var statusCode = exception.GetType().Name switch
        {
            "ValidationException" => StatusCodes.Status400BadRequest,
            "NotFoundException" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Type = exception.GetType().Name,
            Title = "Error occurred",
            Detail = exception.Message,
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
