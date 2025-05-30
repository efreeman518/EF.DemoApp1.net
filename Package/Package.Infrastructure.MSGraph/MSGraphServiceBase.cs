using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Package.Infrastructure.MSGraph.Models;

namespace Package.Infrastructure.MSGraph;

public class MSGraphServiceBase(ILogger<MSGraphServiceBase> logger, IOptions<MSGraphServiceSettingsBase> settings, GraphServiceClient graphClient) : IMSGraphServiceBase
{
    public async Task<User?> GetUserAsync(string userId, string? select = null, string? expand = null)
    {
        logger.LogInformation("Getting user {UserId}", userId);
        return await graphClient.Users[userId].GetAsync(requestConfiguration =>
        {
            if (!string.IsNullOrEmpty(select))
            {
                requestConfiguration.QueryParameters.Select = select.Split(',');
            }
            if (!string.IsNullOrEmpty(expand))
            {
                requestConfiguration.QueryParameters.Expand = expand.Split(',');
            }
        });
    }

    public async Task<string?> UpsertUserAsync(UpsertUserRequest request)
    {
        var user = new User
        {
            AccountEnabled = request.AccountEnabled,
            DisplayName = request.DisplayName,
            PasswordProfile = new PasswordProfile
            {
                Password = request.Password, // Password can be null if not changing for an update
                ForceChangePasswordNextSignIn = request.forceChangePasswordNextSignIn
            }
        };

        if (request.AdditionalData != null)
        {
            user.AdditionalData ??= new Dictionary<string, object>();
            foreach (var kvp in request.AdditionalData)
            {
                string key = kvp.Key.StartsWith("extension_") ? kvp.Key : $"extension_{settings.Value.ExtensionAppObjectId}_{kvp.Key}";
                user.AdditionalData[key] = kvp.Value;
            }
        }

        if (string.IsNullOrEmpty(request.id))
        {
            // Create User
            logger.LogInformation("Creating user {Email} with display name {DisplayName}", request.Email, request.DisplayName);
            ArgumentException.ThrowIfNullOrEmpty(request.Email, nameof(request.Email));
            ArgumentException.ThrowIfNullOrEmpty(request.Password, nameof(request.Password)); // Password is required for new user

            user.MailNickname = request.Email.Split('@')[0];
            user.Identities =
            [
                new ObjectIdentity
                {
                    SignInType = "emailAddress",
                    Issuer = settings.Value.IdentityIssuer, // e.g., contosob2c.onmicrosoft.com
                    IssuerAssignedId = request.Email
                }
            ];
            // UserPrincipalName is not set for B2C local accounts for creation via this method.

            var createdUser = await graphClient.Users.PostAsync(user);
            return createdUser?.Id;
        }
        else
        {
            // Update User
            logger.LogInformation("Updating user {UserId}", request.id);

            // Note: MailNickname and Identities (like email) are typically not updated once created via PATCH,
            // or have specific processes if they need to be changed.
            // UserPrincipalName is not set for B2C local accounts.

            await graphClient.Users[request.id].PatchAsync(user);
            return request.id; // Return the user ID upon successful update
        }
    }

    public async Task DeleteUserAsync(string userId)
    {
        logger.LogInformation("Deleting user {UserId}", userId);
        await graphClient.Users[userId].DeleteAsync();
    }
}
