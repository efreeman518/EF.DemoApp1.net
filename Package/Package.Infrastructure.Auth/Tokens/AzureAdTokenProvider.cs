using LazyCache;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Package.Infrastructure.Http.Tokens;

public class AzureAdTokenProvider : IOAuth2TokenProvider
{
    private readonly IAppCache _appCache;
    private readonly IConfidentialClientApplication _app;

    public AzureAdTokenProvider(IOptions<AzureAdOptions> azureAdOptions, IAppCache appCache)
    {
        _appCache = appCache;
        _app = ConfidentialClientApplicationBuilder.Create(azureAdOptions.Value.ClientId)
            .WithClientSecret(azureAdOptions.Value.ClientSecret)
            .WithAuthority(azureAdOptions.Value.Authority)
            .Build();
    }

    public async Task<string> GetAccessTokenAsync(string[] scopes)
    {
        try
        {
            var accessToken = await _appCache.GetOrAddAsync("access_token", async entry =>
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
}

public class AzureAdOptions
{
    public string Authority { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
}
