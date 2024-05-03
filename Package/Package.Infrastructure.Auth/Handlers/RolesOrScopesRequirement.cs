using Microsoft.AspNetCore.Authorization;

namespace Package.Infrastructure.Auth.Handlers;

public class RolesOrScopesRequirement(string[]? roles = null, string[]? scopes = null) : IAuthorizationRequirement
{
    public string[] Roles { get; set; } = roles ?? [];
    public string[] Scopes { get; set; } = scopes ?? [];
}
