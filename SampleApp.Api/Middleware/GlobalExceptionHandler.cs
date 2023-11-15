using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SampleApp.Api.Middleware;

/// <summary>
/// Middleware is Singleton, so can only inject Singleton (not Scoped)
/// </summary>
/// <param name="next"></param>
/// <param name="loggerFactory"></param>
/// <param name="hostEnvironment"></param>
public sealed class GlobalExceptionHandler(RequestDelegate next, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger _logger = loggerFactory.CreateLogger<GlobalExceptionHandler>();
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            //execute next middleware; catch any exceptions
            await _next(context);
        }
        catch (Exception ex)
        {
            await LogAndSendResponse(context, ex);
        }
    }

    private async Task LogAndSendResponse(HttpContext context, Exception ex)
    {
        try
        {
            _logger.Log(LogLevel.Error, ex, "GlobalExceptionHandlerMiddleware caught exception: {Message}", ex.Message);
        }
        catch (Exception exInternal)
        {
            try
            {
                _logger.Log(LogLevel.Error, exInternal, "GlobalExceptionHandlerMiddleware internal exception when attempting to log an application exception ({Message})", ex.Message);
            }
            catch
            {
                //internal logging error; throw the original
                throw ex;
            }
        }

        //REST Response
        //ex = ex.GetBaseException(); //holds info that caused the exception

        string detail = (ex is ValidationException exFluentValidation)
            ? string.Join(Environment.NewLine, exFluentValidation.Errors)
            : ex.Message;

        context.Response.StatusCode = ex.GetType().Name switch
        {
            "ValidationException" => StatusCodes.Status400BadRequest,
            "NotFoundException" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Title = ex.GetType().Name,
            Detail = detail,
            Status = context.Response.StatusCode
        };
        problemDetails.Extensions.Add("traceidentifier", context.TraceIdentifier);
        if (_hostEnvironment.IsDevelopment())
        {
            problemDetails.Extensions.Add("stacktrace", ex.StackTrace);
        }

        context.Response.ContentType = new MediaTypeHeaderValue("application/json").ToString();
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}

