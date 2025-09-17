using Microsoft.Extensions.Logging;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;         // Beta models
using Microsoft.Kiota.Abstractions;
using System.Net;

namespace Package.Infrastructure.MSGraph;

public class ExternalIdUserFlowService(ILogger<ExternalIdUserFlowService> logger, GraphServiceClient graphClient)
{
    /// <summary>
    /// Ensures a sign-in only b2x user flow exists and is assigned to the given application object (not clientId).
    /// Note: B2xIdentityUserFlow has no DisplayName; the portal shows the 'id' as the friendly name.
    /// </summary>
    public async Task EnsureSignInOnlyUserFlowAssignedAsync(string userFlowId, string displayName, string applicationObjectId)
    {
        ArgumentException.ThrowIfNullOrEmpty(userFlowId);
        ArgumentException.ThrowIfNullOrEmpty(displayName);
        ArgumentException.ThrowIfNullOrEmpty(applicationObjectId);

        // 1) Get or create a sign-in only user flow (beta)
        B2xIdentityUserFlow? flow = null;
        try
        {
            flow = await graphClient.Identity.B2xUserFlows[userFlowId].GetAsync();
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound)
        {
            logger.LogInformation("User flow {UserFlowId} not found. Creating as sign-in only (display '{DisplayName}').", userFlowId, displayName);
        }

        if (flow is null)
        {
            flow = await graphClient.Identity.B2xUserFlows.PostAsync(new B2xIdentityUserFlow
            {
                Id = userFlowId,
                UserFlowType = UserFlowType.SignIn,
                UserFlowTypeVersion = 1f
            });
        }
        else if (flow.UserFlowType != UserFlowType.SignIn)
        {
            throw new InvalidOperationException($"User flow '{userFlowId}' exists but is of type '{flow.UserFlowType}'. Create/use a 'signIn' flow to block sign-up.");
        }

        // 2) Assign the user flow to the application via beta $ref (no SDK nav used)
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            // Let the RequestAdapter inject the base URL; avoids null deref on BaseUrl
            UrlTemplate = "{+baseurl}/identity/b2xUserFlows/{b2xIdentityUserFlow%2Did}/applications/$ref",
        };
        requestInfo.PathParameters["b2xIdentityUserFlow%2Did"] = userFlowId;

        var reference = new ReferenceCreate
        {
            // v1.0 application resource is fine for the $ref body
            OdataId = $"https://graph.microsoft.com/v1.0/applications/{applicationObjectId}"
        };
        requestInfo.SetContentFromParsable(graphClient.RequestAdapter, "application/json", reference);

        try
        {
            await graphClient.RequestAdapter.SendNoContentAsync(requestInfo);
            logger.LogInformation("Assigned user flow {UserFlowId} to application {AppObjectId}.", userFlowId, applicationObjectId);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.Conflict)
        {
            // Already assigned
            logger.LogInformation("User flow {UserFlowId} is already assigned to application {AppObjectId}.", userFlowId, applicationObjectId);
        }
    }
}