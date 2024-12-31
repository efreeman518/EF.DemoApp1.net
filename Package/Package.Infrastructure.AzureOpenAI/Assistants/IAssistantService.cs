using Azure.AI.OpenAI.Assistants;

namespace Package.Infrastructure.AzureOpenAI.Assistants;

// The Assistants feature area is in beta, with API specifics subject to change.
// Suppress the [Experimental] warning via .csproj or, as here, in the code to acknowledge.
// #pragma warning disable OPENAI001

public interface IAssistantService
{
    Task<Assistant> GetOrCreateAssistantByName(string? assistantName = null, AssistantCreationOptions? options = null, CancellationToken cancellationToken = default);
    Task<Assistant> GetAssistantAsync(string assistantId, CancellationToken cancellationToken = default);
    Task<AssistantThread> CreateThreadAsync(AssistantThreadCreationOptions? options = null, CancellationToken cancellationToken = default);
    Task<ThreadRun> CreateThreadAndRunAsync(CreateAndRunThreadOptions options, CancellationToken cancellationToken = default);

    Task<string> AddMessageAndRunThreadAsync(string threadId, string userMessage, CreateRunOptions crOptions,
        Func<IReadOnlyList<RequiredToolCall>, Task<List<ToolOutput>>>? toolCallFunc = null, CancellationToken cancellationToken = default);

    Task<OpenAIFile?> GetFileByFilenameAsync(string filename, CancellationToken cancellationToken);
    Task<OpenAIFile> UploadFileAsync(Stream data, OpenAIFilePurpose purpose, string? filename = null, CancellationToken cancellationToken = default);
    Task<PageableList<AssistantFile>> GetAssistantFilesAsync(string assistantId, int? limit = null, ListSortOrder? listSortOrder = null, string? after = null, string? before = null, CancellationToken cancellationToken = default);
    Task<bool> LinkAssistantFileASync(string assistantId, string fileId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAssisantsAsync(List<string>? keepers = null, CancellationToken cancellationToken = default);

}