using Application.Contracts.Model;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SampleApp.Api.Middleware;

public sealed class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    /// <summary>
    /// Middleware is Singleton, so can only inject Singleton (not Scoped)
    /// </summary>
    /// <param name="next"></param>
    /// <param name="loggerFactory"></param>
    /// <param name="loggingSettings"></param>
    /// <param name="settings"></param>
    public GlobalExceptionHandler(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        _next = next;
        _logger = loggerFactory.CreateLogger<GlobalExceptionHandler>();
    }


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

        context.Response.StatusCode = ex.GetType().Name switch
        {
            "ValidationException" => StatusCodes.Status400BadRequest,
            "NotFoundException" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        ExceptionResponse response = new() { Message = ex.Message };
        context.Response.ContentType = new MediaTypeHeaderValue("application/json").ToString();
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

