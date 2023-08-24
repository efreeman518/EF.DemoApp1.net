using Azure.Identity;
using SampleApp.Api;
using SampleApp.Bootstrapper;
using SampleApp.Bootstrapper.Configuration;

var SERVICE_NAME = "SampleApi";

//logging for initialization
ILogger<Program> loggerStartup;
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);
    builder.AddConsole();
    builder.AddApplicationInsights();
});
loggerStartup = loggerFactory.CreateLogger<Program>();

try
{
    loggerStartup.LogInformation("{ServiceName} - Startup.", SERVICE_NAME);

    //CreateBuilder defaults:
    //- config gets 'ASPNETCORE_*' env vars, appsettings.json and appsettings.{Environment}.json
    //- logging gets Console
    var builder = WebApplication.CreateBuilder(args);

    //set up DefaultAzureCredential for subsequent use in configuration providers
    //https://azuresdkdocs.blob.core.windows.net/$web/dotnet/Azure.Identity/1.8.0/api/Azure.Identity/Azure.Identity.DefaultAzureCredentialOptions.html
    var credentialOptions = new DefaultAzureCredentialOptions();
    //Specifies the client id of a user assigned ManagedIdentity. 
    string? credOptionsManagedIdentity = builder.Configuration.GetValue<string?>("ManagedIdentityClientId", null);
    if (credOptionsManagedIdentity != null) credentialOptions.ManagedIdentityClientId = credOptionsManagedIdentity;
    //Specifies the tenant id of the preferred authentication account, to be retrieved from the shared token cache for single sign on authentication with development tools, in the case multiple accounts are found in the shared token.
    string? credOptionsTenantId = builder.Configuration.GetValue<string?>("SharedTokenCacheTenantId", null);
    if (credOptionsTenantId != null) credentialOptions.SharedTokenCacheTenantId = credOptionsTenantId;
    var credential = new DefaultAzureCredential(credentialOptions);

    string env = builder.Configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") ?? "Development";

    //configuration
    //user secrets
    if (builder.Environment.IsDevelopment()) builder.Configuration.AddUserSecrets<Program>();

    //Azure AppConfig
    var endpoint = builder.Configuration.GetValue<string>("AzureAppConfig:Endpoint");
    if (!string.IsNullOrEmpty(endpoint))
    {
        loggerStartup.LogInformation("{ServiceName} - Add Azure App Configuration {Endpoint} {Environment}", SERVICE_NAME, endpoint, env);
        builder.AddAzureAppConfiguration(endpoint, credential, env, builder.Configuration.GetValue<string>("AzureAppConfig:Sentinel"), new TimeSpan(0, 0, 30));
    }

    //Azure Key Vault - load AKV direct (not through Azure AppConfig or App Service-Configuration-AppSettings)
    endpoint = builder.Configuration.GetValue<string>("KeyVaultEndpoint");
    if (!string.IsNullOrEmpty(endpoint))
    {
        loggerStartup.LogInformation("{ServiceName} - Add KeyVault {Endpoint} Configuration", SERVICE_NAME, endpoint);
        builder.Configuration.AddAzureKeyVault(new Uri(endpoint), credential);
    }

    //Custom configuration provider - from DB
    builder.Configuration.AddEntityConfiguration();

    //logging
    loggerStartup.LogInformation("{ServiceName} - Configure logging.", SERVICE_NAME);
    builder.Logging.ClearProviders();
    builder.Logging.AddApplicationInsights();
    if (builder.Environment.IsDevelopment())
    {
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
    }

    loggerStartup.LogInformation("{ServiceName} - Register services.", SERVICE_NAME);
    var config = builder.Configuration;
    builder.Services
        //api services - controllers, versioning, health checks, swagger, telemetry
        .RegisterApiServices(config)
        //infrastructure - caches, DbContexts, repos, external service proxies, startup tasks
        .RegisterInfrastructureServices(config)
        //domain services
        .RegisterDomainServices(config)
        //app servives
        .RegisterApplicationServices(config)
        //background services
        .RegisterBackgroundServices(config);

    var app = builder.Build().ConfigurePipeline();
    await app.RunStartupTasks();
    await app.RunAsync();
}
catch (Exception ex)
{
    loggerStartup.LogCritical(ex, "{ServiceName} - Host terminated unexpectedly.", SERVICE_NAME);
}
finally
{
    loggerStartup.LogInformation("{ServiceName} - Ending application.", SERVICE_NAME);
}


/// <summary>
/// Program class must be explicitly defined for WebApplicationFactory in Test.Endpoints
/// </summary>
#pragma warning disable S1118
public partial class Program { }
#pragma warning restore S1118