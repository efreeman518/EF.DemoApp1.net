using LanguageExt.Common;
using Package.Infrastructure.BlandAI.Model;

namespace Package.Infrastructure.BlandAI;

public interface IBlandAIRestClient
{
    Task<Result<SendCallResponse?>> SendCallAsync(SendCallRequest request, CancellationToken cancellationToken = default);
    Task<Result<AnalyzeCallResponse?>> AnalyzeCallAsync(string callId, AnalyzeCallRequest request, CancellationToken cancellationToken = default);


    Task<Result<AgentResponse?>> CreateWebAgentAsync(AgentRequest request, CancellationToken cancellationToken = default);
    Task<Result<AgentResponse?>> UpdateWebAgentAsync(AgentRequest request, CancellationToken cancellationToken = default);
    Task<Result<TokenResponse?>> AuthorizeWebAgentCallAsync(string agentId, CancellationToken cancellationToken = default);
    Task<Result<DefaultResponse?>> DeleteWebAgentAsync(string agentId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<AgentResponse>?>> ListWebAgentsAsync(CancellationToken cancellationToken = default);

}
