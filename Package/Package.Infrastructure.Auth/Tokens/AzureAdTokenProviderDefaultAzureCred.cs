using Azure.Core;
using Azure.Identity;
using LazyCache;
using Microsoft.Extensions.Logging;

namespace Package.Infrastructure.Auth.Tokens;

public interface IAzureAdTokenRetriever
{
    Task<string> GetAccessTokenAsync(string resourceId, string scope = "/.default");
}

public class AzureAdTokenProviderDefaultAzureCred : IAzureAdTokenRetriever
{
    private readonly ILogger<AzureAdTokenProviderDefaultAzureCred> _logger;
    private readonly IAppCache _appCache;

    public AzureAdTokenProviderDefaultAzureCred(ILogger<AzureAdTokenProviderDefaultAzureCred> logger, IAppCache appCache)
    {
        _logger = logger;
        _appCache = appCache;
    }

    public async Task<string> GetAccessTokenAsync(string resourceId, string scope = "/.default")
    {
        var resourceIdentifier = resourceId + scope;
        var key = $"access_token:{resourceIdentifier}";
        var accessToken = await _appCache.GetOrAddAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var tokenCredential = new DefaultAzureCredential();
            var accessToken = await tokenCredential.GetTokenAsync(
                new TokenRequestContext(new[] { resourceIdentifier }), CancellationToken.None); //.ConfigureAwait(false);
            return accessToken;
        });

        return accessToken.Token;
    }
}