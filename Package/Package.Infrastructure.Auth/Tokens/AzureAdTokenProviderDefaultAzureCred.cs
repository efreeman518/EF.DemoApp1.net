using Azure.Core;
using Azure.Identity;
using LazyCache;
using Microsoft.Extensions.Logging;

namespace Package.Infrastructure.Auth.Tokens;

public interface IAzureAdTokenRetriever
{
    Task<string> GetAccessTokenAsync(string resourceId, string scope = "/.default", CancellationToken cancellationToken = default);
}

public class AzureAdTokenProviderDefaultAzureCred(ILogger<AzureAdTokenProviderDefaultAzureCred> logger, IAppCache appCache) : IAzureAdTokenRetriever
{
    public async Task<string> GetAccessTokenAsync(string resourceId, string scope = "/.default", CancellationToken cancellationToken = default)
    {
        _ = logger.GetHashCode();

        var resourceIdentifier = resourceId + scope;
        var key = $"access_token:{resourceIdentifier}";
        var accessToken = await appCache.GetOrAddAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var tokenCredential = new DefaultAzureCredential();
            //var accessToken = await tokenCredential.GetTokenAsync(
            //    new TokenRequestContext(new[] { resourceIdentifier }), cancellationToken); //.ConfigureAwait(false);

            //"api://105684a3-a969-4f3e-89f4-3da2ff0b0a16" //.default
            var accessToken = await tokenCredential.GetTokenAsync(
                new TokenRequestContext([scope]),
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