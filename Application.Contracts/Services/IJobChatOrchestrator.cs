using Application.Contracts.Model;
using Package.Infrastructure.Domain;

namespace Application.Contracts.Services;

public interface IJobChatOrchestrator
{
    Task<Result<ChatResponse>> ChatCompletionAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
