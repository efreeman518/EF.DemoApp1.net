using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace SampleApp.Api;

public static class Configuration
{
    /// <summary>
    /// Load configuration from Azure App Config; config setting ASPNETCORE_ENVIRONMENT value will be used to load/override
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="loggerStartup"></param>
    /// <param name="endpoint"></param>
    /// <param name="credential"></param>
    public static void AddAzureAppConfiguration(this WebApplicationBuilder builder, ILogger<Program> loggerStartup,
        string endpoint, DefaultAzureCredential credential)
    {
        string? env = builder.Configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");
        loggerStartup.LogInformation("Add Azure App Configuration {Endpoint} {Environment} Start", endpoint, env);

        builder.Configuration.AddAzureAppConfiguration(options =>
            options.Connect(new Uri(endpoint), credential)
            .Select(KeyFilter.Any, LabelFilter.Null) // Load configuration values with no label
            .Select(KeyFilter.Any, env) // Override with any configuration values specific to current hosting env
            .ConfigureKeyVault(kv =>
            {
                //Integrate with keyvault - https://erwinstaal.nl/posts/azure-app-config-and-azure-keyvault/
                kv.SetCredential(credential);
            })
            .ConfigureRefresh(refresh =>
            {
                refresh.Register("SettingsSentinel", refreshAll: true).SetCacheExpiration(new TimeSpan(0, 5, 0));
            }));
        loggerStartup.LogInformation("Add Azure App Configuration {Endpoint} {Environment} Complete", endpoint, env);
    }

    /// <summary>
    /// Load configuration directly from KeyVault
    /// https://docs.microsoft.com/en-us/azure/app-service/app-service-managed-service-identity
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="loggerStartup"></param>
    /// <param name="endpoint"></param>
    /// <param name="credential"></param>
    public static void AddAzureKeyVaultConfiguration(this WebApplicationBuilder builder, ILogger<Program> loggerStartup,
        string endpoint, DefaultAzureCredential credential)
    {
        loggerStartup.LogInformation("Add KeyVault {Endpoint} Configuration Start", endpoint);
        builder.Configuration.AddAzureKeyVault(new Uri(endpoint), credential);
        loggerStartup.LogInformation("Add KeyVault {Endpoint} Configuration Complete", endpoint);
    }
}
