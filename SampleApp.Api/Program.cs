using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;
using SampleApp.Api;
using SampleApp.Bootstrapper;

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
    //- config gets 'ASPNETCORE_*' env vars, appsettings.json and appsettings.{Environment}.json, user secrets
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

    var env = builder.Configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") ?? "Development";

    //configuration
    string? endpoint;

    //Azure AppConfig
    var appConfig = builder.Configuration.GetSection("AzureAppConfig");
    if (appConfig != null)
    {
        endpoint = appConfig.GetValue<string>("Endpoint");
        loggerStartup.LogInformation("{ServiceName} - Add Azure App Configuration {Endpoint} {Environment}", SERVICE_NAME, endpoint, env);
        builder.AddAzureAppConfiguration(endpoint!, credential, env, appConfig.GetValue<string>("Sentinel"), appConfig.GetValue("RefreshCacheExpireTimeSpan", new TimeSpan(1, 0, 0)));
    }

    //Azure Key Vault - load AKV direct (not through Azure AppConfig or App Service-Configuration-AppSettings)
    endpoint = builder.Configuration.GetValue<string>("KeyVaultEndpoint");
    if (!string.IsNullOrEmpty(endpoint))
    {
        loggerStartup.LogInformation("{ServiceName} - Add KeyVault {Endpoint} Configuration", SERVICE_NAME, endpoint);
        builder.Configuration.AddAzureKeyVault(new Uri(endpoint), credential);
    }

    //Custom configuration provider - from DB
    //var connectionString = builder.Configuration.GetConnectionString("TodoDbContextQuery") ?? "";
    //builder.Configuration.AddDatabaseSource(connectionString, new TimeSpan(1, 0, 0));

    //logging
    loggerStartup.LogInformation("{ServiceName} - Configure logging.", SERVICE_NAME);
    builder.Logging.ClearProviders();
    builder.Logging.AddApplicationInsights();
    if (builder.Environment.IsDevelopment())
    {
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
    }

    //Data Protection - use blobstorage (key file) and keyvault; server farm/instances will all use the same keys
    //register here since credential has been configured
    //https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview
    string? dataProtectionKeysFileUrl = builder.Configuration.GetValue<string?>("DataProtectionKeysFileUrl", null); //blob key file
    string? dataProtectionEncryptionKeyUrl = builder.Configuration.GetValue<string?>("DataProtectionEncryptionKeyUrl", null); //vault encryption key
    if (!string.IsNullOrEmpty(dataProtectionKeysFileUrl) && !string.IsNullOrEmpty(dataProtectionEncryptionKeyUrl))
    {
        loggerStartup.LogInformation("{ServiceName} - Configure Data Protection.", SERVICE_NAME);
        builder.Services.AddDataProtection()
            .PersistKeysToAzureBlobStorage(new Uri(dataProtectionKeysFileUrl), credential)
            .ProtectKeysWithAzureKeyVault(new Uri(dataProtectionEncryptionKeyUrl), credential);
    }

    loggerStartup.LogInformation("{ServiceName} - Register services.", SERVICE_NAME);
    var config = builder.Configuration;
    builder.Services
        //infrastructure - caches, DbContexts, repos, external service sdks/proxies, startup tasks
        .RegisterInfrastructureServices(config, true)
        //domain services
        .RegisterDomainServices(config)
        //app services
        .RegisterApplicationServices(config)
        //background services
        .RegisterBackgroundServices(config)
        //api services - controllers, versioning, health checks, swagger, telemetry
        .RegisterApiServices(config);

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