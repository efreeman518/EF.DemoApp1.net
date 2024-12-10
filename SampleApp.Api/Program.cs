using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Package.Infrastructure.Common;
using Package.Infrastructure.Host;
using SampleApp.Api;
using SampleApp.Bootstrapper;

//CreateBuilder defaults:
//- config gets 'ASPNETCORE_*' env vars, appsettings.json and appsettings.{Environment}.json, user secrets
//- logging gets Console
var builder = WebApplication.CreateBuilder(args);
var appName = builder.Configuration.GetValue<string>("AppName");

//static logger factory setup - for startup
StaticLogging.CreateStaticLoggerFactory(logBuilder =>
{
    logBuilder.SetMinimumLevel(LogLevel.Information);
    logBuilder.AddApplicationInsights(configureTelemetryConfiguration: (config) =>
            config.ConnectionString = builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString"),
            configureApplicationInsightsLoggerOptions: (options) => { });
    logBuilder.AddConsole();
});

//startup logger
ILogger<Program> loggerStartup = StaticLogging.CreateLogger<Program>();
loggerStartup.LogInformation("{AppName} - Startup.", appName);

var env = builder.Configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") ?? "Development";

try
{
    loggerStartup.LogInformation("{AppName} - Configure app logging.", appName);
    builder.Logging
        .ClearProviders()
        .AddApplicationInsights(configureTelemetryConfiguration: (config) =>
            config.ConnectionString = builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString"),
            configureApplicationInsightsLoggerOptions: (options) => { });
    if (builder.Environment.IsDevelopment())
    {
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
    }

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

    //configuration
    string? endpoint;

    //Azure AppConfig
    var appConfig = builder.Configuration.GetSection("AzureAppConfig");
    if (appConfig != null)
    {
        endpoint = appConfig.GetValue<string>("Endpoint");
        loggerStartup.LogInformation("{AppName} - Add Azure App Configuration {Endpoint} {Environment}", appName, endpoint, env);
        builder.AddAzureAppConfiguration(endpoint!, credential, env, appConfig.GetValue<string>("Sentinel"), appConfig.GetValue("RefreshCacheExpireTimeSpan", new TimeSpan(1, 0, 0)));
    }

    //Azure Key Vault - load AKV direct (not through Azure AppConfig or App Service-Configuration-AppSettings)
    endpoint = builder.Configuration.GetValue<string>("KeyVaultEndpoint");
    if (!string.IsNullOrEmpty(endpoint))
    {
        loggerStartup.LogInformation("{AppName} - Add KeyVault {Endpoint} Configuration", appName, endpoint);
        builder.Configuration.AddAzureKeyVault(new Uri(endpoint), credential);
    }

    //Custom configuration provider - from DB
    //var connectionString = builder.Configuration.GetConnectionString("TodoDbContextQuery") ?? "";
    //builder.Configuration.AddDatabaseSource(connectionString, new TimeSpan(1, 0, 0));

    //Data Protection - use blobstorage (key file) and keyvault; server farm/instances will all use the same keys
    //register here since credential has been configured
    //https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview
    string? dataProtectionKeysFileUrl = builder.Configuration.GetValue<string?>("DataProtectionKeysFileUrl", null); //blob key file
    string? dataProtectionEncryptionKeyUrl = builder.Configuration.GetValue<string?>("DataProtectionEncryptionKeyUrl", null); //vault encryption key
    if (!string.IsNullOrEmpty(dataProtectionKeysFileUrl) && !string.IsNullOrEmpty(dataProtectionEncryptionKeyUrl))
    {
        loggerStartup.LogInformation("{AppName} - Configure Data Protection.", appName);
        builder.Services.AddDataProtection()
            .PersistKeysToAzureBlobStorage(new Uri(dataProtectionKeysFileUrl), credential)
            .ProtectKeysWithAzureKeyVault(new Uri(dataProtectionEncryptionKeyUrl), credential);
    }

    loggerStartup.LogInformation("{AppName} - Register services.", appName);
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
        //api services - controllers, versioning, health checks, openapidoc, telemetry
        .RegisterApiServices(config, loggerStartup);

    var app = builder.Build().ConfigurePipeline();
    loggerStartup.LogInformation("{AppName} - Running startup tasks.", appName);
    await app.RunStartupTasks();

    //static logger factory setup - re-configure for application static logging
    StaticLogging.CreateStaticLoggerFactory(logBuilder =>
    {
        logBuilder.SetMinimumLevel(LogLevel.Information);
        logBuilder.ClearProviders();
        logBuilder.AddApplicationInsights(configureTelemetryConfiguration: (config) =>
                config.ConnectionString = builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString"),
                configureApplicationInsightsLoggerOptions: (options) => { });
        if (builder.Environment.IsDevelopment())
        {
            logBuilder.AddConsole();
            logBuilder.AddDebug();
        }
    });

    loggerStartup.LogInformation("{AppName} {Environment} - Running app.", appName, env);

    await app.RunAsync();
}
catch (Exception ex)
{
    loggerStartup.LogCritical(ex, "{AppName} {Environment} - Host terminated unexpectedly.", appName, env);
}
finally
{
    loggerStartup.LogInformation("{AppName} {Environment} - Ending application.", appName, env);
}


/// <summary>
/// Program class must be explicitly defined for WebApplicationFactory in Test.Endpoints
/// </summary>
#pragma warning disable S1118
public partial class Program { }
#pragma warning restore S1118