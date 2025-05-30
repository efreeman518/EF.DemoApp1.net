using Application.Contracts.Interfaces;
using Infrastructure.MSGraphB2C;
using Package.Infrastructure.MSGraph.Models;

namespace Application.Services;
public class B2CManagement(IMSGraphServiceB2C b2cClient) : IB2CManagement
{
    /// <summary>
    /// Creates a B2C user with custom tenantId and roles claims.
    /// The user must change their password at first login.
    /// </summary>
    /// <param name="email">User's email (also used as UPN)</param>
    /// <param name="tenantId">Custom tenantId to store as extension attribute</param>
    /// <param name="roles">Roles to store as extension attribute</param>
    /// <param name="initialPassword">Initial password to set</param>
    /// <returns>The created user's object ID</returns>
    public async Task<string?> CreateUserAsync(string displayName, string email, string userTenantId, IEnumerable<string> roles)
    {
        var additionalData = new Dictionary<string, object>
        {
            { "UserTenantId", userTenantId },
            { "Roles", roles.ToList() }
        };

        var request = new CreateUserRequest(true, displayName, email, "changeOnLoginRequired", additionalData);
        var userId = await b2cClient.CreateUserAsync(request);
        return userId;
    }
}
