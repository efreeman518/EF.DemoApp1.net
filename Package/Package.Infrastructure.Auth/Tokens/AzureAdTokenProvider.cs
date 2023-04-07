using LazyCache;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Package.Infrastructure.Http.Tokens;

public class AzureAdTokenProvider : IOAuth2TokenProvider
{
    private readonly AzureAdOptions _azureAdOptions;
    private readonly IAppCache _memoryCache;
    private readonly IConfidentialClientApplication _app;

    public AzureAdTokenProvider(IOptions<AzureAdOptions> azureAdOptions, IAppCache memoryCache)
    {
        _azureAdOptions = azureAdOptions.Value;
        _memoryCache = memoryCache;

        _app = ConfidentialClientApplicationBuilder.Create(_azureAdOptions.ClientId)
            .WithClientSecret(_azureAdOptions.ClientSecret)
            .WithAuthority(_azureAdOptions.Authority)
            .Build();
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var accessToken = await _memoryCache.GetOrAddAsync("access_token", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            var result = await _app.AcquireTokenForClient(new string[] { _azureAdOptions.Resource + ".default" }).ExecuteAsync();
            return result.AccessToken;
        });

        return accessToken;
    }
}

public class AzureAdOptions
{
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public string Authority { get; set; } = null!;
    public string Resource { get; set; } = null!;
}
