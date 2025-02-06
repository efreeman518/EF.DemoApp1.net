using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.BlandAI.Model;
using Package.Infrastructure.Common.Extensions;

namespace Package.Infrastructure.BlandAI;

public class BlandAIRestClient(ILogger<BlandAIRestClient> logger, IOptions<BlandAIRestClientSettings> settings, HttpClient httpClient) : IBlandAIRestClient
{
    private const string urlSegment = "agents";

    public async Task<Result<AgentResponse?>> CreateWebAgent(AgentRequest request, CancellationToken cancellationToken)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseResultAsync<AgentResponse>(HttpMethod.Post, $"{urlSegment}", request, cancellationToken: cancellationToken);
        return parsedResponse;
    }

    public async Task<Result<AgentResponse?>> UpdateWebAgent(AgentRequest request, CancellationToken cancellationToken)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseResultAsync<AgentResponse>(HttpMethod.Post, $"{urlSegment}/{request.AgentId}", request, cancellationToken: cancellationToken);
        return parsedResponse;
    }

    /// <summary>
    /// Gets a token to be used on the client to initiate a webcall
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<TokenResponse?>> AuthorizeWebAgentCall(string agentId, CancellationToken cancellationToken)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseResultAsync<TokenResponse>(HttpMethod.Post, $"{urlSegment}/{agentId}/authorize", cancellationToken: cancellationToken);
        return parsedResponse; 
    }

    public async Task<Result<DefaultResponse?>> DeleteWebAgent(string agentId, CancellationToken cancellationToken)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseResultAsync<DefaultResponse>(HttpMethod.Post, $"{urlSegment}/{agentId}/delete", cancellationToken: cancellationToken);
        return parsedResponse; 
    }

    public async Task<Result<IReadOnlyList<AgentResponse>?>> ListWebAgents(CancellationToken cancellationToken)
    {
        (var _, var parsedResponse) = await httpClient.HttpRequestAndResponseResultAsync<IReadOnlyList<AgentResponse>>(HttpMethod.Get, $"{urlSegment}", cancellationToken: cancellationToken);
        return parsedResponse;
    }

}
