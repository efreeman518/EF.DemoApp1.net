using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Functions.Infrastructure;
public class GlobalExceptionHandler : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // This is added pre-function execution, function will have access to this information
        // in the context.Items dictionary
        context.Items.Add("middlewareitem", "Hello, from middleware");

        try
        {
            //execute function or next middleware; catch any exceptions
            await next(context);
        }
        catch (Exception ex)
        {
            ILogger logger = context.GetLogger<GlobalExceptionHandler>();
            try
            {
                logger.Log(LogLevel.Error, ex, "GlobalExceptionHandlerMiddleware caught exception: {Error}", ex.Message);
            }
            catch (Exception exInternal)
            {
                try
                {
                    logger.Log(LogLevel.Error, exInternal, "GlobalExceptionHandlerMiddleware internal exception when attempting to log an application exception {Error}", ex.Message);
                }
                catch
                {
                    //internal logging error; throw the original
                    throw exInternal;
                }
            }
        }

        // This happens after function execution. We can inspect the context after the function was invoked
        if (context.Items.TryGetValue("functionitem", out object? value) && value is string message)
        {
            ILogger logger = context.GetLogger<GlobalExceptionHandler>();
            logger.LogInformation("From function: {Message}", message);
        }
    }
}
