using System.Security.Claims;
using System.Text.Json;

namespace SampleApp.Api.Middleware;

public class CustomHeaderAuthMiddleware(RequestDelegate next, ILogger<CustomHeaderAuthMiddleware> logger, string headerName, string userRolesClaimKey)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(headerName, out var headerValues))
        {
            var headerJson = headerValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(headerJson))
            {
                try
                {
                    var claimsFromHeader = JsonSerializer.Deserialize<Dictionary<string, string>>(headerJson);

                    if (claimsFromHeader != null && claimsFromHeader.TryGetValue(userRolesClaimKey, out var rolesJsonString) && !string.IsNullOrEmpty(rolesJsonString))
                    {
                        List<string>? roles = null;
                        try
                        {
                            // Assuming rolesJsonString is a JSON array string like "[\"Admin\",\"User\"]"
                            // This is based on how Azure AD B2C often formats array claims when their .Value is read.
                            roles = JsonSerializer.Deserialize<List<string>>(rolesJsonString);
                        }
                        catch (JsonException ex)
                        {
                            logger.LogWarning(ex, "Failed to deserialize userRoles JSON string from header: {RolesJsonString}. Ensure '{UserRolesClaimKey}' in {HeaderName} is a valid JSON array string e.g., \"[\\\"Admin\\\",\\\"User\\\"]\".", rolesJsonString, userRolesClaimKey, headerName);
                        }

                        if (roles != null && roles.Count != 0)
                        {
                            var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();

                            ClaimsIdentity identityToAugmentOrAdd;

                            if (context.User.Identity != null && context.User.Identity.IsAuthenticated && context.User.Identity is ClaimsIdentity existingClaimsIdentity)
                            {
                                logger.LogDebug("Augmenting existing authenticated identity with roles from {HeaderName}.", headerName);
                                identityToAugmentOrAdd = existingClaimsIdentity;
                                identityToAugmentOrAdd.AddClaims(roleClaims);
                            }
                            //else
                            //{
                            //    logger.LogInformation("No authenticated user or compatible identity found. Creating new identity based on roles from {HeaderName}.", headerName);
                            //    var newIdentity = new ClaimsIdentity(roleClaims, "CustomHeaderAuthentication");
                            //    if (context.User == null || !context.User.Identities.Any(id => id.IsAuthenticated)) // Handles AnonymousPrincipal or no principal
                            //    {
                            //        context.User = new ClaimsPrincipal(newIdentity);
                            //    }
                            //    else // User exists but primary identity wasn't ClaimsIdentity or wasn't authenticated
                            //    {
                            //        context.User.AddIdentity(newIdentity);
                            //    }
                            //}
                        }
                        else
                        {
                            logger.LogInformation("No roles found in '{UserRolesClaimKey}' from {HeaderName} header, or the roles list was empty.", userRolesClaimKey, headerName);
                        }
                    }
                    else
                    {
                        logger.LogInformation("'{UserRolesClaimKey}' key not found in JSON from {HeaderName} header or its value was empty.", userRolesClaimKey, headerName);
                    }
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize {HeaderName} header JSON: {HeaderJson}", headerName, headerJson);
                }
            }
        }
        else
        {
            // This is normal if the request doesn't come via the gateway or the gateway doesn't add the header.
            logger.LogDebug("No {HeaderName} header found.", headerName);
        }

        await next(context);
    }
}

public static class CustomHeaderAuthMiddlewareExtensions
{
    /// <summary>
    /// Adds middleware to process roles from a custom request header.
    /// </summary>
    /// <param name="builder">The IApplicationBuilder instance.</param>
    /// <param name="origRequestHeaderName">The name of the HTTP header containing the JSON claims payload (e.g., "X-Orig-Request").</param>
    /// <param name="userRolesClaimKey">The key within the JSON payload that contains the user roles (e.g., "userRoles").</param>
    /// <returns>The IApplicationBuilder instance.</returns>
    public static IApplicationBuilder UseCustomHeaderAuth(this IApplicationBuilder builder, string origRequestHeaderName = "X-Orig-Request", string userRolesClaimKey = "userRoles")
    {
        if (string.IsNullOrWhiteSpace(origRequestHeaderName))
        {
            throw new ArgumentException("Header name cannot be null or whitespace.", nameof(origRequestHeaderName));
        }
        if (string.IsNullOrWhiteSpace(userRolesClaimKey))
        {
            throw new ArgumentException("User roles claim key cannot be null or whitespace.", nameof(userRolesClaimKey));
        }
        return builder.UseMiddleware<CustomHeaderAuthMiddleware>(origRequestHeaderName, userRolesClaimKey);
    }
}
