using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functions;

/// <summary>
/// local - http://localhost:7071/api/FunctionHttpHealth
/// </summary>
/// <param name="configuration"></param>
/// <param name="logger"></param>
/// <param name="settings"></param>
public class FunctionHttpHealth(ILogger<FunctionHttpHealth> logger, IConfiguration configuration, IOptions<Settings1> settings)
{
    [Function(nameof(FunctionHttpHealth))]
    public async Task<HealthCheckResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    //public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        _ = configuration.GetHashCode();
        _ = settings.GetHashCode();

        logger.Log(LogLevel.Information, "FunctionHttpHealth - Start {Request}", req);
        var status = HealthStatus.Healthy;
        try
        {
            //await some dependent service health checks
            await Task.CompletedTask;
            logger.Log(LogLevel.Information, "FunctionHttpHealth - Finish {Status}", status);
        }
        catch (Exception ex)
        {
            status = HealthStatus.Unhealthy;
            logger.LogError(ex, "FunctionHttpHealth - Error {Status}", status);
        }

        return new HealthCheckResult(status,
            description: $"Function Service is {status}.",
            exception: null,
            data: null);

        //public async Task<HttpResponseData / IActionResult>
        //var response = req.CreateResponse(HttpStatusCode.OK);
        //response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        //response.WriteString("Healthy");
        //return response;

    }
}
