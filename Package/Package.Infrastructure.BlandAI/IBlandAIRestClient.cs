using LanguageExt.Common;
using Package.Infrastructure.BlandAI.Model;

namespace Package.Infrastructure.BlandAI;

public interface IBlandAIRestClient
{
    Task<Result<AgentResponse?>> CreateWebAgent(AgentRequest request, CancellationToken cancellationToken = default);
    Task<Result<AgentResponse?>> UpdateWebAgent(AgentRequest request, CancellationToken cancellationToken = default);
    Task<Result<TokenResponse?>> AuthorizeWebAgentCall(string agentId, CancellationToken cancellationToken = default);
    Task<Result<DefaultResponse?>> DeleteWebAgent(string agentId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<AgentResponse>?>> ListWebAgents(CancellationToken cancellationToken = default);

}
