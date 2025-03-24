using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Net.Http.Headers;

namespace SampleApp.UI1.Services;

public class ApiAuthHandler(IAccessTokenProvider tokenProvider, string[] scopes) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var result = await tokenProvider.RequestAccessToken(new AccessTokenRequestOptions
        {
            Scopes = scopes
        });

        if (result.TryGetToken(out var token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
