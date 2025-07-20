using Application.Contracts.Model;
using Package.Infrastructure.Domain;

namespace Application.Contracts.Services;

public interface IJobAssistantOrchestrator
{
    Task<Result<AssistantResponse>> AssistantRunAsync(AssistantRequest request, CancellationToken cancellationToken = default);
}
