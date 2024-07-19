namespace Package.Infrastructure.Auth.Tokens;
public interface IOAuth2TokenProvider
{
    Task<string> GetAccessTokenAsync(string[] scopes);
}