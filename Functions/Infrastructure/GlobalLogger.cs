using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Functions.Infrastructure;
public class GlobalLogger(ILogger<GlobalLogger> logger): IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var functionName = context.FunctionDefinition.Name;

        logger.LogInformation("Function [{FunctionName}]: triggered {Time}", functionName, TimeProvider.System.GetUtcNow());

        //Mask for sensitive?
        var request = context.BindingContext;
        if (request != null && request.BindingData?.Values != null)
        {
            logger.LogInformation("Function [{FunctionName}]: Request data {Data}", functionName, string.Join(";", request.BindingData.Values));
        }

        await next(context);

        logger.LogInformation("Function [{FunctionName}]: finished {Time}", functionName, TimeProvider.System.GetUtcNow());
    }
}
