namespace SampleApp.Api.Middleware;

/// <summary>
/// Middleware is Singleton, so can only inject Singleton (not Scoped)
/// </summary>
/// <param name="next"></param>
/// <param name="loggerFactory"></param>
/// <param name="hostEnvironment"></param>
internal sealed class SomeMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<SomeMiddleware>();

    /// <summary>
    /// method can accept scoped services
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogDebug("SomeMiddleware invoked");

        //manipulate the request/do something before next middleware
        _ = hostEnvironment.GetHashCode();

        //execute next middleware
        await next(context);
    }
}

/// <summary>
/// pipeline convenience extension
/// </summary>
public static class SomeMiddlewareiddlewareExtensions
{
    public static IApplicationBuilder UseSomeMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SomeMiddleware>();
    }
}

