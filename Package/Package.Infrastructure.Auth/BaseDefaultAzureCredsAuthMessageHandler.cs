using Azure.Core;
using Azure.Identity;
using System.Net.Http.Headers;

namespace Package.Infrastructure.Auth;

/* Using managed identity (DefaultAzureCredential) to call another App Service/Function
 * Register the MessageHandler, and add it to the HttpClient
 * Inject HttpClient to the service where it is used
 
 services
  .AddScoped<InheritFromBaseDefaultCredsAuthMessageHandler>()
  .AddHttpClient<ClassUsingHttpClient>((serviceProvider, httpClient) => 
  {
    httpClient.BaseAddress = "https://[appname].azurewebsites.net/api/[targetfunctionname]";
  })
  .AddHttpMessageHandler<InheritFromBaseDefaultCredsAuthMessageHandler>();  
 */

public abstract class BaseDefaultAzureCredsAuthMessageHandler : DelegatingHandler
{
    private readonly TokenRequestContext TokenRequestContext;
    private readonly DefaultAzureCredential Credentials;

    protected BaseDefaultAzureCredsAuthMessageHandler(string[] scopes)
    {
        //TokenRequestContext supports other options
        //This parameter is a list of scopes; if your target App Service/Function has defined scopes then use them here.
        TokenRequestContext = new(scopes);
        Credentials = new DefaultAzureCredential();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        //DefaultAzureCredential caches internally and knows when to refresh
        var tokenResult = await Credentials.GetTokenAsync(TokenRequestContext, cancellationToken);
        var authorizationHeader = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
        request.Headers.Authorization = authorizationHeader;
        return await base.SendAsync(request, cancellationToken);
    }
}
