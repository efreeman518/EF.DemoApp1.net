using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functions;

/// <summary>
/// Hitting a url will trigger this function
/// local debug - the url is shown in the console when run: HttpTrigger: [GET,POST] http://localhost:7071/api/HttpTrigger
/// or use ngrok (./ngrok http http://localhost:7071) if hitting the http trigger from external
/// azure - navigate to the function and click 'Get Function Url': https://[function-app-name].azurewebsites.net/api/HttpTrigger?code=xyz...
/// </summary>
public class FunctionHttpTrigger
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FunctionHttpTrigger> _logger;
    private readonly Settings1 _settings;

    public FunctionHttpTrigger(IConfiguration configuration, ILoggerFactory loggerFactory,
        IOptions<Settings1> settings)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<FunctionHttpTrigger>();
        _settings = settings.Value;
    }

    //https://blog.bredvid.no/patterns-for-securing-your-azure-functions-2fef634f4020
    //local/debug - auth is disabled
    //azure - include the function key in the x-functions-key HTTP request header
    [Function("HttpTrigger")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
    {
        _ = _configuration.GetHashCode();
        _ = _settings.GetHashCode();

        string url = req.Url.ToString();
        _logger.Log(LogLevel.Information, "HttpTrigger - Start url: {url}", url);
        _ = await new StreamReader(req.Body).ReadToEndAsync();

        //await some service call

        _logger.Log(LogLevel.Information, "HttpTrigger - Finish url: {url}", url);

        var responseMessage = $"HttpTrigger completed {DateTime.UtcNow}";
        var response = new OkObjectResult(responseMessage);
        return response;
    }
}
