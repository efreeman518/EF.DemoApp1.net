using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace Package.Infrastructure.Auth.Handlers;

public class RolesOrScopesAuthorizationHandler : AuthorizationHandler<RolesOrScopesRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RolesOrScopesRequirement requirement)
    {
        if (context.User.Claims.Any(c =>
            //https://learn.microsoft.com/en-us/dotnet/api/microsoft.identity.web.claimconstants?view=msal-model-dotnet-latest
            (c.Type == ClaimTypes.Role && (requirement.Roles.Contains(c.Value)))
            ||
            (c.Type == ClaimConstants.Scope || c.Type == ClaimConstants.Scp) && (requirement.Scopes.Contains(c.Value))))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
