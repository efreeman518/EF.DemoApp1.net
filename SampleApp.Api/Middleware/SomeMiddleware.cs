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

    public async Task Invoke(HttpContext context)
    {
        try
        
        {
            //do something before next middleware
            _ = hostEnvironment.GetHashCode();

            //execute next middleware; catch any exceptions
            await next(context);
        }
        catch (Exception ex)
        {
            try
            {
                _logger.LogError(ex, "SomeMiddleware exception: {Message}", ex.Message);
            }
            catch (Exception exInternal)
            {
                try
                {
                    _logger.Log(LogLevel.Error, exInternal, "SomeMiddleware internal exception when attempting to log an application exception ({Message})", ex.Message);
                }
                catch
                {
                    //internal logging error; throw the original
                    throw ex;
                }
            }
        }
    }
}

