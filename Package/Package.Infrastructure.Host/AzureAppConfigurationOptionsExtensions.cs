using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace Package.Infrastructure.Host;

internal static class AzureAppConfigurationOptionsExtensions
{
    internal static void ConfigurePackageDefaults(
        this AzureAppConfigurationOptions options,
        string endpoint,
        DefaultAzureCredential credential,
        string env,
        string? sentinelSetting,
        TimeSpan? cacheExpire,
        IEnumerable<string> keyPrefixes)
    {
        options.Connect(new Uri(endpoint), credential);

        foreach (var prefix in keyPrefixes
                     .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
                     .Select(prefix => prefix.TrimEnd(':') + ":")
                     .Distinct())
        {
            options.Select($"{prefix}*", LabelFilter.Null).TrimKeyPrefix(prefix);
            options.Select($"{prefix}*", env).TrimKeyPrefix(prefix);
        }

        options.ConfigureKeyVault(keyVault =>
        {
            keyVault.SetCredential(credential);
        });

        if (!string.IsNullOrWhiteSpace(sentinelSetting))
        {
            options.ConfigureRefresh(refresh =>
            {
                refresh
                    .Register(sentinelSetting, refreshAll: true)
                    .SetRefreshInterval(cacheExpire ?? new TimeSpan(1, 0, 0));
            });
        }
    }
}
