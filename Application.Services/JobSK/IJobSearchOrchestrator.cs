using LanguageExt.Common;

namespace Application.Services.JobSK;

public interface IJobSearchOrchestrator
{
    Task<Result<ChatResponse>> ChatCompletionAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
