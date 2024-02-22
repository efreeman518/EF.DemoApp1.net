using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Http.Tokens;

namespace Test.Endpoints;

/// <summary>
/// Testing http endpoints (MVC controllers, razor pages)
/// </summary>
[TestClass]
public abstract class EndpointTestBase
{
    protected static readonly IConfigurationRoot _config = Support.Utility.BuildConfiguration().AddUserSecrets<Program>().Build();
    protected readonly IAppCache _appcache;
    protected readonly IOAuth2TokenProvider _tokenProvider;

    protected EndpointTestBase()
    {
        _appcache = new CachingService();
        var options = new AzureADOptions
        {
            TenantId = _config.GetValue<Guid>("Auth:TenantId")!,
            ClientId = _config.GetValue<string>("Auth:ClientId")!,
            ClientSecret = _config.GetValue<string>("Auth:ClientSecret")!
        };
        _ = options.GetHashCode();

        //no auth - not currently used
        //_tokenProvider = new AzureAdTokenProvider(Options.Create(options), _appcache);
        _tokenProvider = null!;
    }

    //no auth - not currently used
    protected async Task ApplyBearerAuthHeader(HttpClient client)
    {
        //scopes = new string[] { _azureAdOptions.Resource + ".default" };
        var scopes = _config.GetValue("Auth:Scopes", new string[] { string.Empty })!;
        var token = await _tokenProvider.GetAccessTokenAsync(scopes);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

}
