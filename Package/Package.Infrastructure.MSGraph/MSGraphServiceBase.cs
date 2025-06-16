using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Package.Infrastructure.MSGraph.Models;

namespace Package.Infrastructure.MSGraph;

/// <summary>
/// https://learn.microsoft.com/en-us/graph/api/resources/user?view=graph-rest-1.0
/// </summary>
/// <param name="logger"></param>
/// <param name="settings"></param>
/// <param name="graphClient"></param>
public class MSGraphServiceBase(ILogger<MSGraphServiceBase> logger, IOptions<MSGraphServiceSettingsBase> settings, GraphServiceClient graphClient) : IMSGraphServiceBase
{
    public async Task<User?> GetUserAsync(string userId, List<string>? select = null, List<string>? expand = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId); // User ID is required

        select ??= ["id", "accountEnabled", "displayName", "mailNickname", "mail", "identities"]; // Default properties to select
        // Define the custom attributes to fetch
        var customAttributes = new List<string>
        {
            $"extension_{settings.Value.ExtensionAppObjectId}_UserTenantId",
            $"extension_{settings.Value.ExtensionAppObjectId}_UserRoles"
        };
        select.AddRange(customAttributes);

        logger.LogInformation("Getting user {UserId}", userId);
        //if select is null, get default properties

        var user = await graphClient.Users[userId].GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Select = [.. select];
            if (expand is not null)
            {
                requestConfiguration.QueryParameters.Expand = [.. expand];
            }
        });

        return user;
    }

    public async Task<string?> CreateUserAsync(GraphUserRequest request)
    {
        logger.LogInformation("Creating user {Email} with display name {DisplayName}", request.Email, request.DisplayName);
        ArgumentException.ThrowIfNullOrEmpty(request.Email);
        ArgumentException.ThrowIfNullOrEmpty(request.Password); // Password is required for new user

        var user = new User
        {
            AccountEnabled = request.AccountEnabled,
            DisplayName = request.DisplayName,
            Mail = request.Email, // Mail is not used for B2C local accounts but can be set
            MailNickname = request.Email.Split('@')[0],
            PasswordProfile = new PasswordProfile
            {
                Password = request.Password, // Password can be null if not changing for an update
                ForceChangePasswordNextSignIn = request.ForceChangePasswordNextSignIn
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
            user.AdditionalData ??= new Dictionary<string, object>();
            foreach (var kvp in request.AdditionalData)
            {
                string key = kvp.Key.StartsWith("extension_") ? kvp.Key : $"extension_{settings.Value.ExtensionAppObjectId}_{kvp.Key}";
                user.AdditionalData[key] = kvp.Value;
            }
        }

        var createdUser = await graphClient.Users.PostAsync(user);
        return createdUser?.Id;
    }

    /// <summary>
    /// Updates an existing user. If the user does not exist, an exception is thrown. 
    /// Identities are not updated, as they are immutable in Azure AD B2C, so changing email will NOT change the identity.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task UpdateUserAsync(GraphUserRequest request)
    {
        logger.LogInformation("Updating user {Email} with display name {DisplayName}", request.Email, request.DisplayName);
        ArgumentException.ThrowIfNullOrEmpty(request.Id); // User ID is required for update

        // Retrieve the existing user
        var existingUser = await GetUserAsync(request.Id) ?? throw new InvalidOperationException($"User with ID {request.Id} not found.");

        // Update only the properties that are populated in the request
        var userToUpdate = new User
        {
            Id = request.Id,
            AccountEnabled = request.AccountEnabled,
            DisplayName = !string.IsNullOrEmpty(request.DisplayName) ? request.DisplayName : existingUser.DisplayName,
            Mail = !string.IsNullOrEmpty(request.Email) ? request.Email : existingUser.Mail,
            MailNickname = !string.IsNullOrEmpty(request.Email) ? request.Email.Split('@')[0] : existingUser.MailNickname
        };

        if (request.AdditionalData != null)
        {
            userToUpdate.AdditionalData ??= new Dictionary<string, object>();
            foreach (var kvp in request.AdditionalData)
            {
                string key = kvp.Key.StartsWith("extension_") ? kvp.Key : $"extension_{settings.Value.ExtensionAppObjectId}_{kvp.Key}";
                userToUpdate.AdditionalData[key] = kvp.Value;
            }
        }
        await graphClient.Users[request.Id].PatchAsync(userToUpdate);

        //update password separately if provided
        if (request.ForceChangePasswordNextSignIn)
        {
            userToUpdate = new User
            {
                PasswordProfile = new PasswordProfile
                {
                    //Password = request.Password,
                    ForceChangePasswordNextSignIn = request.ForceChangePasswordNextSignIn
                }
            };
            await graphClient.Users[request.Id].PatchAsync(userToUpdate);
        }
    }

    public async Task DeleteUserAsync(string userId)
    {
        logger.LogInformation("Deleting user {UserId}", userId);
        await graphClient.Users[userId].DeleteAsync();
    }

    /// <summary>
    /// when the user changes email for login
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="newEmail"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task ChangeUserIdentityAsync(string userId, string newEmail)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId); // User ID is required
        ArgumentException.ThrowIfNullOrEmpty(newEmail); // New email is required

        logger.LogInformation("Changing user identity for {UserId} to new email {NewEmail}", userId, newEmail);
        var user = await GetUserAsync(userId) ?? throw new InvalidOperationException($"User with ID {userId} not found.");

        // Update the identity
        user.Identities = [new ObjectIdentity
        {
            SignInType = "emailAddress",
            Issuer = settings.Value.IdentityIssuer,
            IssuerAssignedId = newEmail
        }];

        await graphClient.Users[userId].PatchAsync(user);
    }
}
