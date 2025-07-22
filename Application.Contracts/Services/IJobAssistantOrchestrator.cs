using Application.Contracts.Model;
using Package.Infrastructure.Common.Contracts;

namespace Application.Contracts.Services;

public interface IJobAssistantOrchestrator
{
    Task<Result<AssistantResponse>> AssistantRunAsync(AssistantRequest request, CancellationToken cancellationToken = default);
}
