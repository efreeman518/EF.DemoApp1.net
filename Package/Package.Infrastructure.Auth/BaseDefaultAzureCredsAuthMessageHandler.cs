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

public abstract class BaseDefaultAzureCredsAuthMessageHandler(string[] scopes) : DelegatingHandler
{
    private readonly TokenRequestContext TokenRequestContext = new(scopes);
    private readonly DefaultAzureCredential Credentials = new(true);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        //DefaultAzureCredential caches internally and knows when to refresh
        var accessToken = await Credentials.GetTokenAsync(TokenRequestContext, cancellationToken);
        var authorizationHeader = new AuthenticationHeaderValue("Bearer", accessToken.Token);
        request.Headers.Authorization = authorizationHeader;
        return await base.SendAsync(request, cancellationToken);
    }
}
