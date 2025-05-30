using Azure.Core;
using Azure.Identity;
using System.Collections.Concurrent;

namespace SampleApp.Gateway;

public class TokenService(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly DefaultAzureCredential _credential = new();
    private static readonly ConcurrentDictionary<string, (string Token, DateTimeOffset Expiry)> _tokenCache = new();

    /// <summary>
    /// Gets a token for this api to call downstream api using DefaultAzureCredential (managed identity, etc)
    /// </summary>
    /// <param name="clusterId"></param>
    /// <returns></returns>
    public async Task<string> GetAccessTokenAsync(string clusterId)
    {
        if (string.IsNullOrEmpty(clusterId)) return string.Empty;

        var apiConfig = _configuration.GetSection(clusterId);
        if (!apiConfig.Exists()) return string.Empty;

        var scopes = apiConfig.GetSection("Scopes").Get<string[]>();
        if (scopes is null || scopes.Length == 0) return string.Empty;

        // Check cache first
        if (_tokenCache.TryGetValue(clusterId, out var cachedToken) && DateTime.UtcNow < cachedToken.Expiry)
        {
            return cachedToken.Token;
        }

        // Acquire token using DefaultAzureCredential
        var tokenRequestContext = new TokenRequestContext(scopes);
        var accessToken = await _credential.GetTokenAsync(tokenRequestContext);

        // Cache the token with buffer before expiry
        _tokenCache[clusterId] = (accessToken.Token, accessToken.ExpiresOn.AddSeconds(-60));

        return accessToken.Token;
    }

}
