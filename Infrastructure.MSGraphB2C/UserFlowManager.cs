//using Azure.Core;
//using Microsoft.Extensions.Logging;
//using Microsoft.Graph;
//using Microsoft.Graph.Models;
//using System;
//using System.Collections.Generic;
//using System.Net.Http.Headers;
//using System.Text.Json;
//using System.Threading.Tasks;

//namespace Infrastructure.MSGraphB2C;

///// <summary>
///// Utilities to inspect and update user flows across the Entra External ID (identity/userFlows)
///// and the B2C (identity/b2cUserFlows) surfaces using GraphServiceClient.
///// </summary>
//public class UserFlowManager
//{
//    private readonly GraphServiceClient _graphClient;
//    private readonly ILogger<UserFlowManager> _logger;

//    public UserFlowManager(GraphServiceClient graphClient, ILogger<UserFlowManager> logger)
//    {
//        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
//        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//    }

//    /*
//     *      PATCH https://graph.microsoft.com/beta/identity/authenticationEventsFlows/{user-flow-id}
// Content-type: application/json

// {
//     "@odata.type": "#microsoft.graph.externalUsersSelfServiceSignUpEventsFlow",
//     "onInteractiveAuthFlowStart": {
//         "@odata.type": "#microsoft.graph.onInteractiveAuthFlowStartExternalUsersSelfServiceSignUp",
//         "isSignUpAllowed": false
//     }
// }

//     */

//    /// <summary>
//    /// Attempt to disable sign-up for a user flow by setting the property
//    /// "userRegistrationEnabled" = false. Tries /identity/userFlows then /identity/b2cUserFlows.
//    /// Returns true if the patch succeeded on either path.
//    /// </summary>
//    //public async Task<bool> DisableSignUpAsync(string userFlowId)
//    //{
//    //    ArgumentException.ThrowIfNullOrEmpty(userFlowId);

//    //    _logger.LogInformation("Attempting to disable sign-up for user flow {FlowId}", userFlowId);

//    //    // First try the Entra External ID surface: identity/userFlows
//    //    try
//    //    {
//    //        var update = new IdentityUserFlow
//    //        {
//    //            AdditionalData = new Dictionary<string, object>
//    //            {
//    //                ["userRegistrationEnabled"] = false
//    //            }
//    //        };

//    //        //await _graphClient.Identity.UserFlows[userFlowId].PatchAsync(update);
//    //        await _graphClient.Identity.UserFlowAttributes.[userFlowId].PatchAsync(update);
//    //        _logger.LogInformation("Patched identity/userFlows/{FlowId}", userFlowId);
//    //        return true;
//    //    }
//    //    catch (Exception exUserFlows)
//    //    {
//    //        _logger.LogDebug(exUserFlows, "identity/userFlows patch failed for {FlowId}, trying b2cUserFlows", userFlowId);
//    //    }

//    //    // Fallback to B2C surface: identity/b2cUserFlows
//    //    try
//    //    {
//    //        var updateB2c = new B2cIdentityUserFlow
//    //        {
//    //            AdditionalData = new Dictionary<string, object>
//    //            {
//    //                ["userRegistrationEnabled"] = false
//    //            }
//    //        };

//    //        await _graphClient.Identity.B2cUserFlows[userFlowId].PatchAsync(updateB2c);
//    //        _logger.LogInformation("Patched identity/b2cUserFlows/{FlowId}", userFlowId);
//    //        return true;
//    //    }
//    //    catch (Exception exB2c)
//    //    {
//    //        _logger.LogError(exB2c, "Failed to patch both identity/userFlows and identity/b2cUserFlows for {FlowId}", userFlowId);
//    //        return false;
//    //    }
//    //}

//    ///// <summary>
//    ///// Get a flow object. Tries Entra External ID first then B2C.
//    ///// Inspect the returned object's AdditionalData to see raw/unknown fields.
//    ///// Returns the typed model (IdentityUserFlow or B2cIdentityUserFlow) as object.
//    ///// </summary>
//    //public async Task<object?> GetFlowAsync(string userFlowId)
//    //{
//    //    ArgumentException.ThrowIfNullOrEmpty(userFlowId);

//    //    try
//    //    {
//    //        var flow = await _graphClient.Identity.UserFlows[userFlowId].GetAsync();
//    //        _logger.LogDebug("Fetched user flow from identity/userFlows: {Id}", userFlowId);
//    //        return flow;
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        _logger.LogDebug(ex, "identity/userFlows GET failed for {FlowId}, trying b2cUserFlows", userFlowId);
//    //    }

//    //    try
//    //    {
//    //        var b2cFlow = await _graphClient.Identity.B2cUserFlows[userFlowId].GetAsync();
//    //        _logger.LogDebug("Fetched user flow from identity/b2cUserFlows: {Id}", userFlowId);
//    //        return b2cFlow;
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        _logger.LogError(ex, "Failed to GET user flow from both endpoints for {FlowId}", userFlowId);
//    //        return null;
//    //    }
//    //}

//    ///// <summary>
//    ///// List available flows. Tries the Entra surface first; falls back to B2C.
//    ///// Returns the raw IEnumerable of models as object so the caller can cast/inspect.
//    ///// </summary>
//    //public async Task<IEnumerable<object>> ListFlowsAsync()
//    //{
//    //    try
//    //    {
//    //        var page = await _graphClient.Identity.UserFlows.GetAsync();
//    //        var list = page?.Value ?? Array.Empty<IdentityUserFlow>();
//    //        _logger.LogDebug("Listed identity/userFlows, count={Count}", list.Count);
//    //        return list;
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        _logger.LogDebug(ex, "identity/userFlows list failed, trying b2cUserFlows");
//    //    }

//    //    try
//    //    {
//    //        var page = await _graphClient.Identity.B2cUserFlows.GetAsync();
//    //        var list = page?.Value ?? Array.Empty<B2cIdentityUserFlow>();
//    //        _logger.LogDebug("Listed identity/b2cUserFlows, count={Count}", list.Count);
//    //        return list;
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        _logger.LogError(ex, "Failed to list user flows on both endpoints");
//    //        return Array.Empty<object>();
//    //    }
//    //}
//}


///// <summary>
///// Utilities to inspect and update user flows across the Entra External ID (identity/userFlows)
///// and the B2C (identity/b2cUserFlows) surfaces using direct Graph REST calls.
///// This avoids compile-time reliance on generated SDK members that may not exist.
///// </summary>
//public class UserFlowManager2
//{
//    private readonly HttpClient _http;
//    private readonly TokenCredential _credential;
//    private readonly ILogger<UserFlowManager> _logger;
//    private const string GraphBase = "https://graph.microsoft.com/v1.0";

//    public UserFlowManager2(ILogger<UserFlowManager> logger)
//        : this(new HttpClient(), new DefaultAzureCredential(), logger)
//    {
//    }

//    // Allow DI to provide HttpClient / TokenCredential if desired.
//    public UserFlowManager2(HttpClient httpClient, TokenCredential credential, ILogger<UserFlowManager> logger)
//    {
//        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
//        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
//        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//    }

//    private async Task AuthenticateHttpAsync(CancellationToken cancellationToken = default)
//    {
//        var token = await _credential.GetTokenAsync(new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }), cancellationToken);
//        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
//    }

//    private static readonly string[] CandidatePaths =
//    {
//            "/identity/userFlows/",      // Entra External ID
//            "/identity/b2cUserFlows/"    // B2C legacy surface
//        };

//    private async Task<(string? pathUsed, HttpResponseMessage? response)> TryGetFlowAsync(string flowId)
//    {
//        for (var i = 0; i < CandidatePaths.Length; i++)
//        {
//            var url = GraphBase + CandidatePaths[i] + Uri.EscapeDataString(flowId);
//            _logger.LogDebug("Probing {Url}", url);
//            var resp = await _http.GetAsync(url);
//            if (resp.IsSuccessStatusCode)
//            {
//                return (CandidatePaths[i], resp);
//            }
//            _logger.LogDebug("Probe failed for {Url} -> {Status}", url, resp.StatusCode);
//        }

//        return (null, null);
//    }

//    /// <summary>
//    /// Attempts to disable sign-up for the user flow by setting the specified property key to the provided value.
//    /// By default it will set "userRegistrationEnabled" = false.
//    /// Returns true if PATCH succeeded.
//    /// </summary>
//    public async Task<bool> DisableSignUpAsync(string userFlowId, string propertyKey = "userRegistrationEnabled")
//    {
//        ArgumentException.ThrowIfNullOrEmpty(userFlowId);

//        _logger.LogInformation("Attempting to disable sign-up for user flow {FlowId}", userFlowId);

//        await AuthenticateHttpAsync();

//        var (path, getResp) = await TryGetFlowAsync(userFlowId);
//        if (path is null || getResp is null)
//        {
//            _logger.LogWarning("User flow {FlowId} not found on known endpoints.", userFlowId);
//            return false;
//        }

//        // Log the GET JSON (inspect to confirm the right property name)
//        var body = await getResp.Content.ReadAsStringAsync();
//        _logger.LogDebug("Existing flow JSON for {FlowId} (path {Path}): {Json}", userFlowId, path, body);

//        // Build patch payload using JSON merge to set the property
//        var patchObj = new Dictionary<string, object>
//        {
//            [propertyKey] = false
//        };
//        var patchJson = JsonSerializer.Serialize(patchObj);
//        var patchContent = new StringContent(patchJson, Encoding.UTF8, "application/json");

//        var patchUrl = GraphBase + path + Uri.EscapeDataString(userFlowId);
//        _logger.LogInformation("PATCHing {PatchUrl} with {Payload}", patchUrl, patchJson);

//        var patchResp = await _http.PatchAsync(patchUrl, patchContent);
//        if (!patchResp.IsSuccessStatusCode)
//        {
//            var err = await patchResp.Content.ReadAsStringAsync();
//            _logger.LogError("Failed to patch user flow. Status: {Status}. Body: {Body}", patchResp.StatusCode, err);
//            return false;
//        }

//        _logger.LogInformation("Successfully patched flow {FlowId} on {Path}", userFlowId, path);
//        return true;
//    }

//    /// <summary>
//    /// Fetches the raw JSON for a flow. Returns null if not found.
//    /// </summary>
//    public async Task<string?> GetFlowJsonAsync(string userFlowId)
//    {
//        ArgumentException.ThrowIfNullOrEmpty(userFlowId);
//        await AuthenticateHttpAsync();

//        var (path, getResp) = await TryGetFlowAsync(userFlowId);
//        if (path is null || getResp is null) return null;

//        return await getResp.Content.ReadAsStringAsync();
//    }

//    /// <summary>
//    /// Lists flows from the first endpoint that responds successfully.
//    /// </summary>
//    public async Task<string?> ListFlowsJsonAsync()
//    {
//        await AuthenticateHttpAsync();
//        foreach (var candidate in CandidatePaths)
//        {
//            var url = GraphBase + candidate;
//            _logger.LogDebug("Listing flows at {Url}", url);
//            var resp = await _http.GetAsync(url);
//            if (resp.IsSuccessStatusCode)
//            {
//                return await resp.Content.ReadAsStringAsync();
//            }
//        }

//        return null;
//    }
//}

//// HttpClient extension for PATCH (available in newer frameworks; included for completeness)
//internal static class HttpClientPatchExtension
//{
//    public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content)
//    {
//        var request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri) { Content = content };
//        return client.SendAsync(request);
//    }
//}