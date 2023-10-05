using LazyCache;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Package.Infrastructure.Http.Tokens;
public class Auth0TokenProvider(IOptions<Auth0Options> auth0Options, IAppCache appCache) : IOAuth2TokenProvider
{
    private readonly Auth0Options _auth0Options = auth0Options.Value;

    public async Task<string> GetAccessTokenAsync(string[] scopes)
    {
        var accessToken = await appCache.GetOrAddAsync("access_token", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            //todo: inject typed client
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_auth0Options.ClientId}:{_auth0Options.ClientSecret}")));

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("audience", _auth0Options.Audience)
            });

            var response = await httpClient.PostAsync($"https://{_auth0Options.Domain}/oauth/token", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<Auth0TokenResponse>(responseContent);

            return tokenResponse!.AccessToken;
        });

        return accessToken;
    }

    private sealed class Auth0TokenResponse
    {
        public string AccessToken { get; set; } = null!;
    }
}

public class Auth0Options
{
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public string Domain { get; set; } = null!;
    public string Audience { get; set; } = null!;
}
