using Azure.Core;
using Azure.Identity;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using static System.Formats.Asn1.AsnWriter;

namespace Package.Infrastructure.Auth.Tokens;

public interface IAzureAdTokenRetriever
{
    Task<string> GetAccessTokenAsync(string resourceId, string scope = "/.default");
}

public class AzureAdTokenRetriever : IAzureAdTokenRetriever
{
    private readonly ILogger<AzureAdTokenRetriever> _logger;
    private readonly IAppCache _appCache;

    public AzureAdTokenRetriever(ILogger<AzureAdTokenRetriever> logger, IAppCache appCache)
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