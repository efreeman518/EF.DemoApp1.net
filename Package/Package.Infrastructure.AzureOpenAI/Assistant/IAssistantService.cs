using OpenAI.Assistants;

namespace Package.Infrastructure.AzureOpenAI.Assistant;

// The Assistants feature area is in beta, with API specifics subject to change.
// Suppress the [Experimental] warning via .csproj or, as here, in the code to acknowledge.
#pragma warning disable OPENAI001

public interface IAssistantService
{
    Task<(string, string)> CreateAssistandAndThreadAsync(string initMessage, AssistantCreationOptions? options = null, CancellationToken cancellationToken = default);

    Task<string> RunAsync(string assistantId, string threadId, RunCreationOptions? options = null,
        Func<IReadOnlyList<ToolDefinition>, Task>? toolCallFunc = null, CancellationToken cancellationToken = default);

}