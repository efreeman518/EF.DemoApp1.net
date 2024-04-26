using Azure.Core;
using Azure.Identity;
using LazyCache;

namespace Package.Infrastructure.Auth.Tokens;

public interface IAzureDefaultCredTokenProvider
{
    Task<string> GetAccessTokenAsync(string resourceId, string scope = ".default", int cacheDurationSeconds = 300, CancellationToken cancellationToken = default);
}

public class AzureDefaultCredTokenProvider(IAppCache appCache) : IAzureDefaultCredTokenProvider
{
    public async Task<string> GetAccessTokenAsync(string resourceId, string scope = ".default", int cacheDurationSeconds = 300, CancellationToken cancellationToken = default)
    {
        var resourceIdentifier = $"{resourceId}/{scope}";
        var key = $"access_token:{resourceIdentifier}";
        var accessToken = await appCache.GetOrAddAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheDurationSeconds);
            var tokenCredential = new DefaultAzureCredential();
            var accessToken = await tokenCredential.GetTokenAsync(
                new TokenRequestContext([resourceIdentifier]),
                cancellationToken
            );
            return accessToken;
        });

        return accessToken.Token;
    }

    /* //https://svrooij.io/2022/04/21/access-api-with-managed-identity/
     * // To request a token using managed identity, the scope has to be the "give me a token with all granted permissions" default. (App URI ID + .default)
        var scope = "api://d788405b-c575-41d1-83a9-83e8cbab062e/.default";
        var tokenCredential = new ManagedIdentityCredential();
        var tokenResponse = await tokenCredential.GetTokenAsync( new TokenRequestContext(new[] { scope }), cancellationToken);
     */
}