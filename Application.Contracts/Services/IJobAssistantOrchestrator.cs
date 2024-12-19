using Application.Contracts.Model;
using LanguageExt.Common;

namespace Application.Contracts.Services;

public interface IJobAssistantOrchestrator
{
    Task<Result<AssistantResponse>> AssistantRunAsync(AssistantRequest request, CancellationToken cancellationToken = default);
}
