namespace Package.Infrastructure.Http.Tokens;
public interface IOAuth2TokenProvider
{
    Task<string> GetAccessTokenAsync(string[] scopes);
}