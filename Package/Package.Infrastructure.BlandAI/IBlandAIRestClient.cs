using LanguageExt.Common;
using Package.Infrastructure.BlandAI.Model;

namespace Package.Infrastructure.BlandAI;

public interface IBlandAIRestClient
{
    Task<Result<AgentResponse?>> CreateWebAgent(AgentRequest request, CancellationToken cancellationToken);
    Task<Result<AgentResponse?>> UpdateWebAgent(AgentRequest request, CancellationToken cancellationToken);
    Task<Result<TokenResponse?>> AuthorizeWebAgentCall(string agentId, CancellationToken cancellationToken);
    Task<Result<DefaultResponse?>> DeleteWebAgent(string agentId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<AgentResponse>?>> ListWebAgents(CancellationToken cancellationToken);

}
