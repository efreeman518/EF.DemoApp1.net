using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace SampleApp.Api;

public static partial class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Load configuration from Azure App Config
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="endpoint"></param>
    /// <param name="credential"></param>
    /// <param name="env"></param>
    /// <param name="sentinelSetting">Observe for refreshing config cache</param>
    /// <param name="cacheExpire">used with sentinelSetting, Timespan to expire cache, default 5 minutes when null</param>
    public static void AddAzureAppConfiguration(this WebApplicationBuilder builder, string endpoint,
        DefaultAzureCredential credential, string env, string? sentinelSetting = null, TimeSpan? cacheExpire = null)
    {
        builder.Configuration.AddAzureAppConfiguration(options =>
        {
            options.Connect(new Uri(endpoint), credential)
            .Select(KeyFilter.Any, LabelFilter.Null) // Load configuration values with no label
            .Select(KeyFilter.Any, env) // Override with any configuration values specific to current hosting env
            .ConfigureKeyVault(kv =>
            {
                //app config may contain references to keyvault
                kv.SetCredential(credential);
            });
            if (sentinelSetting != null)
            {
                options.ConfigureRefresh(refresh =>
                {
                    refresh.Register(sentinelSetting, refreshAll: true).SetCacheExpiration(cacheExpire ?? new TimeSpan(1, 0, 0));
                });
            }
        });
    }
}
