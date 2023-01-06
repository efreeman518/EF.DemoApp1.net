using Functions.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Functions;

/// <summary>
/// https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide
/// net7 now supported in isolated mode - https://devblogs.microsoft.com/dotnet/dotnet-7-comes-to-azure-functions/
/// </summary>
/// 
public class Program
{
    private const string ServiceName = "Functions net7/v4";
    private static ILogger<Program> _logger = null!;
    private static IConfigurationRoot _config = null!;

    protected Program()
    {
    }

    public static async Task Main()
    {
        //logging for initialization
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });
        _logger = loggerFactory.CreateLogger<Program>();

        try
        {
            Log(LogLevel.Information, null, $"Starting {ServiceName}.");

            var host = new HostBuilder()
                .ConfigureHostConfiguration(ConfigConfiguration)
                .ConfigureServices(services =>
                {
                    //app insights telemetry logging for non-http service
                    services.AddApplicationInsightsTelemetryWorkerService(_config);
                    services.AddTransient<IDatabaseService, DatabaseService>();
                    services.Configure<Settings1>(_config.GetSection("Settings1"));
                })
                .ConfigureFunctionsWorkerDefaults(workerApplication =>
                {
                    workerApplication.UseMiddleware<GlobalExceptionHandler>();
                })
                .Build();

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log(LogLevel.Critical, ex, $"{ServiceName} Host terminated unexpectedly.");
        }
        finally
        {
            Log(LogLevel.Information, null, $"Ending worker {ServiceName}.");
        }
    }

    private static void ConfigConfiguration(IConfigurationBuilder builder)
    {
        builder
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
          .AddEnvironmentVariables(); //load AppService - Configuration - AppSettings

        _config = builder.Build();
    }

    private static void Log(LogLevel logLevel, Exception? ex = null, string message = "", params object?[] args)
    {
        _logger?.Log(logLevel, ex, message, args);
    }
}