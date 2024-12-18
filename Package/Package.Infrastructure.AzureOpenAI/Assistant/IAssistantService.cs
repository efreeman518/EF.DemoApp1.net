using Azure.AI.OpenAI.Assistants;

namespace Package.Infrastructure.AzureOpenAI.Assistant;

// The Assistants feature area is in beta, with API specifics subject to change.
// Suppress the [Experimental] warning via .csproj or, as here, in the code to acknowledge.
// #pragma warning disable OPENAI001

public interface IAssistantService
{
    Task<(string, string)> CreateAssistandAndThreadAsync(AssistantCreationOptions? aOptions = null, AssistantThreadCreationOptions? tOptions = null, CancellationToken cancellationToken = default);

    Task<string> AddMessageAndRunThreadAsync(string threadId, string userMessage, CreateRunOptions crOptions,
        Func<IReadOnlyList<RequiredToolCall>, Task<List<ToolOutput>>>? toolCallFunc = null, CancellationToken cancellationToken = default);

}