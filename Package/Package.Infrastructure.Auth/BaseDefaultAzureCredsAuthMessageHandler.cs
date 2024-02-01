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

/// <summary>
/// In the http client SendAsync pipeline, this handler will get a token from the DefaultAzureCredential and add it to the request header
/// then call the next handler in the pipeline - base.SendAsync
/// </summary>
/// <param name="scopes"></param>
public abstract class BaseDefaultAzureCredsAuthMessageHandler(string[] scopes) : DelegatingHandler
{
    private readonly TokenRequestContext TokenRequestContext = new(scopes);
    private readonly DefaultAzureCredential Credentials = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        //DefaultAzureCredential caches internally and knows when to refresh
        var accessToken = await Credentials.GetTokenAsync(TokenRequestContext, cancellationToken);
        var authorizationHeader = new AuthenticationHeaderValue("Bearer", accessToken.Token);
        request.Headers.Authorization = authorizationHeader;
        return await base.SendAsync(request, cancellationToken);
    }
}
