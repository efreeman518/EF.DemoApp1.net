using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;

namespace Package.Infrastructure.Utility;
public static class AuthTokenProvider
{
    /// <summary>
    /// Calls IConfidentialClientApplication.AcquireTokenForClient(scopes).ExecuteAsync()
    /// </summary>
    /// <param name="authority"></param>
    /// <param name="clientId"></param>
    /// <param name="clientSecret"></param>
    /// <param name="scopes"></param>
    /// <returns></returns>
    public static async Task<AuthenticationResult?> GetAuthResultClient(string authority, string clientId, string clientSecret, string[] scopes)
    {
        IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientId)
           .WithClientSecret(clientSecret)
           .WithAuthority(new Uri(authority))
           .Build();

        try
        {
            return await app.AcquireTokenForClient(scopes).ExecuteAsync();
        }
        catch (MsalUiRequiredException ex1)
        {
            _ = ex1.GetHashCode();
            // The application doesn't have sufficient permissions.
            // - AAD - Assign app permissions for the app
            // - AAD - tenant admin grant permissions to the application
            throw;
        }
        catch (MsalServiceException ex2) when (ex2.Message.Contains("AADSTS70011"))
        {
            _ = ex2.GetHashCode();
            // Invalid scope. The scope has to be in the form "https://resourceurl/.default"
            // Mitigation: Change the scope to be as expected.
            throw;
        }
    }
}
