using Azure.Core;
using Azure.Identity;
using System.Net.Http.Headers;

namespace Package.Infrastructure.Auth;

/* Using managed identity (DefaultAzureCredential) to call another App Service/Function
 * Register the MessageHandler, and add it to the HttpClient
 * Inject HttpClient to the service where it is used
 
 services
  .AddScoped<AzureDefaultCredentialsAuthorizationMessageHandler>()
  .AddHttpClient<YourClassUsingTheHttpClient>((serviceProvider, httpClient) => 
  {
    httpClient.BaseAddress = "https://yourfunctionappname.azurewebsites.net/api/targetfunctionname";
  }).AddHttpMessageHandler<AzureDefaultCredentialsAuthorizationMessageHandler>();  
 */

public class AzureDefaultCredentialsAuthorizationMessageHandler : DelegatingHandler
{
    private readonly TokenRequestContext TokenRequestContext;
    private readonly DefaultAzureCredential Credentials;

    public AzureDefaultCredentialsAuthorizationMessageHandler()
    {
        //TokenRequestContext supports other options
        //This parameter is a list of scopes; if your target App Service/Function has defined scopes then you should use them here.
        TokenRequestContext = new(new[] { "targetAADAppRegistrationApplicationId" });
        Credentials = new DefaultAzureCredential();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokenResult = await Credentials.GetTokenAsync(TokenRequestContext, cancellationToken);
        var authorizationHeader = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
        request.Headers.Authorization = authorizationHeader;
        return await base.SendAsync(request, cancellationToken);
    }
}
