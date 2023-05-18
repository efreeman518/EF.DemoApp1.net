using Azure.Core;
using Azure.Identity;
using LazyCache;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Package.Infrastructure.Http.Tokens;

public class AzureAdTokenProviderConfidentialClientApp : IOAuth2TokenProvider
{
    private readonly IAppCache _appCache;

    //todo - make this a thread safe collection (ConcurrentDictionary) to hold multiple target app tokens
    private readonly IConfidentialClientApplication _app;

    public AzureAdTokenProviderConfidentialClientApp(IOptions<AzureAdOptions> azureAdOptions, IAppCache appCache)
    {
        _appCache = appCache;

        //ConfidentialClientApplicationBuilder has many options
        _app = ConfidentialClientApplicationBuilder.Create(azureAdOptions.Value.ClientId)
            .WithClientSecret(azureAdOptions.Value.ClientSecret)
            .WithAuthority(azureAdOptions.Value.Authority)
            .Build();
    }

    public async Task<string> GetAccessTokenAsync(string[] scopes)
    {
        var key = $"access_token:{string.Join(",", scopes)}";
        try
        {
            var accessToken = await _appCache.GetOrAddAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                var result = await _app.AcquireTokenForClient(scopes).ExecuteAsync();
                return result.AccessToken;
            });

            return accessToken;
        }
        catch (MsalUiRequiredException ex1)
        {
            _ = ex1.GetHashCode();
            // The application doesn't have sufficient permissions.
            // - AAD - Assign app permissions for the app
            // - AAD - tenant admin grant permissions to the application
            throw;
        }
        catch (MsalServiceException ex2) when (ex2.Message.Contains("AADSTS70011"))
        {
            _ = ex2.GetHashCode();
            // Invalid scope. The scope has to be in the form "https://resourceurl/.default"
            // Mitigation: Change the scope to be as expected.
            throw;
        }
    }

    /// <summary>
    /// Get token for an Azure resource using built in DefaultAzureCredential
    /// Need to define an "App Role" in the target app registration's manifest. This is the app registration which is used to represent the resource (taget api App Service).
    /// Then you use the Azure CLI to grant permission for that "App Role" to the Enterprise App (The one generated when you setup a managed identity for the client app)
    /// https://blog.yannickreekmans.be/secretless-applications-add-permissions-to-a-managed-identity/
    /// </summary>
    /// <param name="resourceId"></param>
    /// <param name="scope"></param>
    /// <returns></returns>
    public async Task<string> GetTokenAsync(string resourceId, string scope = "/.default")
    {
        var resourceIdentifier = resourceId + scope;
        var token = await _appCache.GetOrAddAsync(resourceIdentifier, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var tokenCredential = new DefaultAzureCredential();
            var accessToken = await tokenCredential.GetTokenAsync(new TokenRequestContext(new[] { resourceIdentifier }), CancellationToken.None);
            return accessToken.Token;
        });

        return token;
    }
}

public class AzureAdOptions
{
    public string Authority { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
}
