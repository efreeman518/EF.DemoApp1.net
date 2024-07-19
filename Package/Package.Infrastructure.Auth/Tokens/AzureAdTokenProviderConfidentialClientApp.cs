using Azure.Core;
using Azure.Identity;
using LazyCache;
using Microsoft.Identity.Client;

namespace Package.Infrastructure.Auth.Tokens;

public class AzureAdTokenProviderConfidentialClientApp(AzureADOptions azureAdOptions, IAppCache appCache) //: IOAuth2TokenProvider
{

    //todo - make this a thread safe collection (ConcurrentDictionary) to hold multiple target app tokens
    private readonly IConfidentialClientApplication _app = ConfidentialClientApplicationBuilder.Create(azureAdOptions.ClientId)
            .WithClientSecret(azureAdOptions.ClientSecret)
            .WithAuthority(azureAdOptions.AADInstance, azureAdOptions.TenantId)
            //.WithAuthority("https://login.microsoftonline.com/c32ce235-4d9a-4296-a647-a9edb2912ac9")
            .Build();

    public async Task<string> GetAccessTokenAsync(string[] scopes, bool forceRefresh = false)
    {
        var key = $"access_token:{string.Join(",", scopes)}";
        try
        {
            var accessToken = await appCache.GetOrAddAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                var result = await _app.AcquireTokenForClient(scopes).WithForceRefresh(forceRefresh).ExecuteAsync();
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
    /// Need to define an "App Role" in the target app registration's manifest. 
    /// This is the app registration which is used to represent the resource (target api App Service).
    /// Then you use the Azure CLI to grant permission for that "App Role" to the Enterprise App 
    /// (The one generated when you setup a managed identity for the client app)
    /// https://blog.yannickreekmans.be/secretless-applications-add-permissions-to-a-managed-identity/
    /// </summary>
    /// <param name="resourceId"></param>
    /// <param name="scope"></param>
    /// <returns></returns>
    public async Task<string> GetTokenAsync(string resourceId, string scope = "/.default")
    {
        var resourceIdentifier = resourceId + scope;
        var token = await appCache.GetOrAddAsync(resourceIdentifier, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var tokenCredential = new DefaultAzureCredential();
            var accessToken = await tokenCredential.GetTokenAsync(new TokenRequestContext([resourceIdentifier]), CancellationToken.None);
            return accessToken.Token;
        });

        return token;
    }
}

public class AzureADOptions
{
    public AzureCloudInstance AADInstance { get; set; } = AzureCloudInstance.AzurePublic;
    public Guid TenantId { get; set; }
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
}
