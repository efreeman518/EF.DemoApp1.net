using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.BlandAI.Model;
using Package.Infrastructure.Common.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI;

public class BlandAIRestClient(ILogger<BlandAIRestClient> logger, IOptions<BlandAISettings> settings, HttpClient httpClient) : IBlandAIRestClient
{
    private const string urlCalls = "calls";
    private const string urlAgents = "agents";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };

    #region calls

    public async Task<Result<SendCallResponse?>> SendCallAsync(SendCallRequest request, CancellationToken cancellationToken = default)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseResultAsync<SendCallResponse>(HttpMethod.Post, $"{urlCalls}", request, JsonSerializerOptions, ensureSuccessStatusCode: false, cancellationToken: cancellationToken);
        return parsedResponse;
    }

    public async Task<Result<AnalyzeCallResponse?>> AnalyzeCallAsync(string callId, AnalyzeCallRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"{urlCalls}/{callId}/analyze";
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseResultAsync<AnalyzeCallResponse>(HttpMethod.Post, $"{url}", request, JsonSerializerOptions, ensureSuccessStatusCode: false, cancellationToken: cancellationToken);
        return parsedResponse;
    }


    #endregion

    #region agents

    public async Task<Result<AgentResponse?>> CreateWebAgentAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseResultAsync<AgentResponse>(HttpMethod.Post, $"{urlAgents}", request, cancellationToken: cancellationToken);
        return parsedResponse;
    }

    public async Task<Result<AgentResponse?>> UpdateWebAgentAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseResultAsync<AgentResponse>(HttpMethod.Post, $"{urlAgents}/{request.AgentId}", request, cancellationToken: cancellationToken);
        return parsedResponse;
    }

    /// <summary>
    /// Gets a token to be used on the client to initiate a webcall
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<TokenResponse?>> AuthorizeWebAgentCallAsync(string agentId, CancellationToken cancellationToken = default)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseResultAsync<TokenResponse>(HttpMethod.Post, $"{urlAgents}/{agentId}/authorize", cancellationToken: cancellationToken);
        return parsedResponse; 
    }

    public async Task<Result<DefaultResponse?>> DeleteWebAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseResultAsync<DefaultResponse>(HttpMethod.Post, $"{urlAgents}/{agentId}/delete", cancellationToken: cancellationToken);
        return parsedResponse; 
    }

    public async Task<Result<IReadOnlyList<AgentResponse>?>> ListWebAgentsAsync(CancellationToken cancellationToken = default)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseResultAsync<IReadOnlyList<AgentResponse>>(HttpMethod.Get, $"{urlAgents}", cancellationToken: cancellationToken);
        return parsedResponse;
    }

    #endregion

}
