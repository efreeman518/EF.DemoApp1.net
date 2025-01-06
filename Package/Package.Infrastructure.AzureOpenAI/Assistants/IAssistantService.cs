using OpenAI.Assistants;

namespace Package.Infrastructure.AzureOpenAI.Assistants;

// The Assistants feature area is in beta, with API specifics subject to change.
// Suppress the [Experimental] warning via .csproj or, as here, in the code to acknowledge.
#pragma warning disable OPENAI001

public interface IAssistantService
{
    Task<Assistant> GetOrCreateAssistantByName(string? assistantName = null, AssistantCreationOptions? options = null, CancellationToken cancellationToken = default);
    Task<Assistant> GetAssistantAsync(string assistantId, CancellationToken cancellationToken = default);
    Task<AssistantThread> CreateThreadAsync(ThreadCreationOptions? options = null, CancellationToken cancellationToken = default);
    Task<ThreadRun> CreateThreadAndRunAsync(string assistantId, ThreadCreationOptions? tOptions = null, RunCreationOptions? rOptions = null, CancellationToken cancellationToken = default);
    Task<string> AddMessageAndRunThreadAsync(string assistantId, string threadId, string message, MessageCreationOptions? mOptions = null, RunCreationOptions? rOptions = null,
        Func<IReadOnlyList<RequiredAction>, Task<List<ToolOutput>>>? toolCallFunc = null, CancellationToken cancellationToken = default);

    Task<bool> DeleteAssistantsAsync(List<string>? keepers = null, CancellationToken cancellationToken = default);

    Task<string?> GetFileIdByFilenameAsync(string filename, CancellationToken cancellationToken);
    Task<string?> UploadFileAsync(Stream fileStream, string filename, CancellationToken cancellationToken);

}

#pragma warning restore OPENAI001