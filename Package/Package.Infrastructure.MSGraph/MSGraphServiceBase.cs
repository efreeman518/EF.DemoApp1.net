using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Package.Infrastructure.MSGraph.Models;

namespace Package.Infrastructure.MSGraph;

public class MSGraphServiceBase(ILogger<MSGraphServiceBase> logger, IOptions<MSGraphServiceSettingsBase> settings, GraphServiceClient graphClient) : IMSGraphServiceBase
{
    public async Task<string?> CreateUserAsync(CreateUserRequest request)
    {
        logger.LogInformation("Creating user {Email} with display name {DisplayName}", request.Email, request.DisplayName);
        var user = new User
        {
            AccountEnabled = request.AccountEnabled,
            DisplayName = request.DisplayName,
            MailNickname = request.Email.Split('@')[0],
            //UserPrincipalName = request.Email, // Do not set UserPrincipalName for B2C local accounts
            PasswordProfile = new PasswordProfile
            {
                ForceChangePasswordNextSignIn = true,
                Password = request.Password
            },
            Identities =
            [
                new ObjectIdentity
                {
                    SignInType = "emailAddress",
                    Issuer = settings.Value.IdentityIssuer, // e.g., contosob2c.onmicrosoft.com
                    IssuerAssignedId = request.Email
                }
            ]
        };
        if (request.AdditionalData != null)
        {
            //can be in claims for user's access token
            user.AdditionalData ??= new Dictionary<string, object>();
            foreach (var kvp in request.AdditionalData)
            {
                // Add custom properties to AdditionalData
                string key = kvp.Key.StartsWith("extension_") ? kvp.Key : $"extension_{settings.Value.ExtensionAppObjectId}_{kvp.Key}";
                user.AdditionalData[key] = kvp.Value;
            }
        }
        var createdUser = await graphClient.Users.PostAsync(user);
        return createdUser?.Id;
    }
}
