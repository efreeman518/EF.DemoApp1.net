using Azure;
using Azure.AI.OpenAI.Assistants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
//using OpenAI.Files;
using System.Text;

namespace Package.Infrastructure.AzureOpenAI.Assistants;

// The Assistants feature area is in beta, with API specifics subject to change.
// Suppress the [Experimental] warning via .csproj or, as here, in the code to acknowledge.
// #pragma warning disable OPENAI001

// requires higher token limits than the default
// gpt-4o version 2024-05-13 (Default) testing worked with Rate limit (Tokens per minute) 50,000 Rate limit(Requests per minute) 300

/// <summary>
/// Assistants are stateful, more features than chat
/// https://www.nuget.org/packages/Azure.AI.OpenAI.Assistants/1.0.0-beta.4#show-readme-container
/// https://github.com/Azure/azure-sdk-for-net/blob/Azure.AI.OpenAI.Assistants_1.0.0-beta.4/sdk/openai/Azure.AI.OpenAI.Assistants/README.md
/// https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md#use-assistants-and-stream-a-run
/// https://platform.openai.com/docs/assistants/overview
/// </summary>
/// <param name="logger"></param>
/// <param name="settings"></param>
/// <param name="client"></param>
public abstract class AssistantServiceBase(ILogger<AssistantServiceBase> logger, IOptions<AssistantServiceSettingsBase> settings, AssistantsClient client) : IAssistantService
{
    //Azure client factory does not currently support this AssistantsClient
    //private readonly AssistantClient client = clientFactory.CreateClient(settings.Value.ResourceName).GetAssistantClient();

    //assistant
    public async Task<PageableList<Assistant>> GetAssistantsAsync(int limit, ListSortOrder? listSortOrder = null, string? after = null, string? before = null, CancellationToken cancellationToken = default) =>
        (await client.GetAssistantsAsync(limit, listSortOrder, after, before, cancellationToken)).Value;
    public async Task<Assistant> GetAssistantAsync(string assistantId, CancellationToken cancellationToken = default) => 
        (await client.GetAssistantAsync(assistantId, cancellationToken)).Value;
    public async Task<Assistant> CreateAssistantAsync(AssistantCreationOptions? options = null, CancellationToken cancellationToken = default) => 
        (await client.CreateAssistantAsync(options, cancellationToken)).Value;
    public async Task<Assistant> UpdateAssistantAsync(string assistantId, UpdateAssistantOptions options, CancellationToken cancellationToken = default) =>
        (await client.UpdateAssistantAsync(assistantId, options, cancellationToken)).Value;
    public async Task<PageableList<AssistantFile>> UpdateAssistantAsync(string assistantId, int limit, ListSortOrder? listSortOrder = null, string? after = null, string? before = null, CancellationToken cancellationToken = default) =>
        (await client.GetAssistantFilesAsync(assistantId, limit, listSortOrder, after, before, cancellationToken)).Value;
    public async Task<bool> DeleteAssistantAsync(string assistantId, CancellationToken cancellationToken = default) =>
        (await client.DeleteAssistantAsync(assistantId, cancellationToken)).Value;

    //threads
    public async Task<AssistantThread> GetThread(string threadId, CancellationToken cancellationToken = default) =>
        (await client.GetThreadAsync(threadId, cancellationToken)).Value;
    public async Task<AssistantThread> CreateThreadAsync(AssistantThreadCreationOptions? options = null, CancellationToken cancellationToken = default)
    {
        return options == null
             ? (await client.CreateThreadAsync(cancellationToken)).Value
             : (await client.CreateThreadAsync(options, cancellationToken)).Value;
    }

    public async Task<ThreadRun> CreateThreadAndRunAsync(CreateAndRunThreadOptions options, CancellationToken cancellationToken = default) =>
        (await client.CreateThreadAndRunAsync(options, cancellationToken)).Value;
    public async Task<AssistantThread> UpdateThreadAsync(string threadId, IDictionary<string,string>? metadata = null, CancellationToken cancellationToken = default) =>
        (await client.UpdateThreadAsync(threadId, metadata, cancellationToken)).Value;
    public async Task<bool> DeleteThreadAsync(string threadId, CancellationToken cancellationToken = default) =>
        (await client.DeleteThreadAsync(threadId, cancellationToken)).Value;
    
    //messages
    public async Task<PageableList<ThreadMessage>> GetMessagesAsync(string threadId, int limit, ListSortOrder? listSortOrder = null, string? after = null, string? before = null, CancellationToken cancellationToken = default) =>
        (await client.GetMessagesAsync(threadId, limit, listSortOrder, after, before, cancellationToken)).Value;
    public async Task<ThreadMessage> GetMessageAsync(string threadId, string messageId, CancellationToken cancellationToken = default) =>
        (await client.GetMessageAsync(threadId, messageId, cancellationToken)).Value;
    public async Task<ThreadMessage> CreateMessageAsync(string threadId, MessageRole role, string content, CancellationToken cancellationToken = default) =>
        (await client.CreateMessageAsync(threadId, role, content, cancellationToken: cancellationToken)).Value;
    public async Task<ThreadMessage> UpdateMessage(string threadId, string messageId, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
        (await client.UpdateMessageAsync(threadId, messageId, metadata, cancellationToken)).Value;

    //runs
    public async Task<PageableList<ThreadRun>> GetRunsAsync(string threadId, int limit, ListSortOrder? listSortOrder = null, string? after = null, string? before = null, CancellationToken cancellationToken = default) =>
        (await client.GetRunsAsync(threadId, limit, listSortOrder, after, before, cancellationToken)).Value;
    public async Task<ThreadRun> GetRunAsync(string threadId, string runId, CancellationToken cancellationToken = default) =>
        (await client.GetRunAsync(threadId, runId, cancellationToken)).Value;
    public async Task<ThreadRun> CreateRunAsync(string threadId, CreateRunOptions options, CancellationToken cancellationToken = default) =>
        (await client.CreateRunAsync(threadId, options, cancellationToken)).Value;
    public async Task<ThreadRun> UpdateRunAsync(string threadId, string runId, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
        (await client.UpdateRunAsync(threadId, runId, metadata, cancellationToken)).Value;
    public async Task<ThreadRun> SubmitToolOutputsToRunAsync(ThreadRun threadRun, List<ToolOutput> toolOutputs, CancellationToken cancellationToken = default) =>
        (await client.SubmitToolOutputsToRunAsync(threadRun, toolOutputs, cancellationToken)).Value;
    public async Task<ThreadRun> CancelRunAsync(string threadId, string runId, CancellationToken cancellationToken = default) =>
        (await client.CancelRunAsync(threadId, runId, cancellationToken)).Value;

    //RunSteps
    public async Task<PageableList<RunStep>> GetRunStepsRunAsync(ThreadRun threadRun, int limit, ListSortOrder? listSortOrder = null, string? after = null, string? before = null, CancellationToken cancellationToken = default) =>
        (await client.GetRunStepsAsync(threadRun, limit, listSortOrder, after, before, cancellationToken)).Value;
    public async Task<RunStep> GetRunStepAsync(string threadId, string runId, string stepId, CancellationToken cancellationToken = default) =>
        (await client.GetRunStepAsync(threadId, runId, stepId, cancellationToken)).Value;

    //Files
    public async Task<PageableList<AssistantFile>> GetAssistantFilesAsync(string assistantId, int? limit = null, ListSortOrder? listSortOrder = null, string? after = null, string? before = null, CancellationToken cancellationToken = default) =>
        (await client.GetAssistantFilesAsync(assistantId, limit, listSortOrder, after, before, cancellationToken)).Value;
    public async Task<AssistantFile> GetAssistantFileAsync(string assistantId, string fileId, CancellationToken cancellationToken = default) =>
        (await client.GetAssistantFileAsync(assistantId, fileId, cancellationToken)).Value;
    public async Task<IReadOnlyList<OpenAIFile>> GetFilesAsync(OpenAIFilePurpose purpose, CancellationToken cancellationToken = default) =>
        (await client.GetFilesAsync(purpose, cancellationToken)).Value;
    public async Task<OpenAIFile> GetFileAsync(string fileId, CancellationToken cancellationToken = default) =>
        (await client.GetFileAsync(fileId, cancellationToken)).Value;
    public async Task<bool> DeleteFileAsync(string fileId, CancellationToken cancellationToken = default) =>
        (await client.DeleteFileAsync(fileId, cancellationToken)).Value;
    public async Task<OpenAIFile> UploadFileAsync(Stream data, OpenAIFilePurpose purpose, string? filename = null, CancellationToken cancellationToken = default) =>
        (await client.UploadFileAsync(data, purpose, filename, cancellationToken)).Value;
    public async Task<bool> LinkAssistantFileASync(string assistantId, string fileId, CancellationToken cancellationToken = default) =>
        (await client.UnlinkAssistantFileAsync(assistantId, fileId, cancellationToken)).Value;

    public async Task<Assistant> GetOrCreateAssistantByName(string? assistantName = null, AssistantCreationOptions? options = null, CancellationToken cancellationToken = default)
    {
        Assistant? assistant = null;

        if(assistantName != null)
        {
            assistant = await GetAssistantByNameAsync(assistantName, cancellationToken); 
        }

        if (assistant == null)
        {
            assistant = await CreateAssistantAsync(options, cancellationToken);
        }
        
        return assistant;
    }


    /// <summary>
    /// Assumes max 100 asistants
    /// </summary>
    /// <param name="name"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<Assistant?> GetAssistantByNameAsync(string name, CancellationToken cancellationToken)
    {
        var assistants = await GetAssistantsAsync(100, cancellationToken: cancellationToken);
        return assistants.Data.FirstOrDefault(a => a.Name == name); // ?? throw new InvalidOperationException($"Assistant with name {name} not found.");
    }

    public async Task<OpenAIFile?> GetFileByFilenameAsync(string filename, CancellationToken cancellationToken)
    {
        var files = await GetFilesAsync(OpenAIFilePurpose.Assistants, cancellationToken: cancellationToken);
        return files.FirstOrDefault(f => f.Filename == filename); // ?? throw new InvalidOperationException($"File with name {name} not found.");
    }

    public async Task<bool> DeleteAssisantsAsync(List<string>? keepers = null, CancellationToken cancellationToken = default)
    {
        var assistants = GetAssistantsAsync(100, cancellationToken: cancellationToken).Result;
        foreach (var assistant in assistants.Data)
        {
            
            if (keepers == null || !(keepers.Contains(assistant.Id) || keepers.Contains(assistant.Name)))
            {
                await DeleteAssistantAsync(assistant.Id, cancellationToken);
            }
        }
        return true;
    }

    //public async Task<(string, string)> CreateAssistandAndThreadAsync(AssistantCreationOptions? aOptions = null, AssistantThreadCreationOptions? tOptions = null, CancellationToken cancellationToken = default)
    //{
    //    AssistantThread thread = tOptions == null
    //        ? (await client.CreateThreadAsync(cancellationToken)).Value
    //        : (await client.CreateThreadAsync(tOptions, cancellationToken)).Value;

    //    logger.LogInformation("Assistant created: {AssistantId}, Thread created: {ThreadId}", assistant.Id, thread.Id);
    //    return (assistant.Id, thread.Id);
    //}

    public async Task<string> AddMessageAndRunThreadAsync(string threadId, string userMessage, CreateRunOptions crOptions,
        Func<IReadOnlyList<RequiredToolCall>, Task<List<ToolOutput>>>? toolCallFunc = null, CancellationToken cancellationToken = default)
    {
        _ = await client.CreateMessageAsync(threadId, MessageRole.User, userMessage, cancellationToken: cancellationToken);
        ThreadRun threadRun = (await client.CreateRunAsync(threadId, crOptions, cancellationToken)).Value;

        logger.LogInformation("Assistant {AssistandId} Thread {ThreadId} ThreadRun {ThreadRunId} created. Starting to poll.", crOptions.AssistantId, threadId, threadRun.Id);

        //poll the thread run (and process tool calls) until it's in an end state
        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(settings.Value.RunThreadPollingDelayMilliseconds), cancellationToken);
            threadRun = (await client.GetRunAsync(threadId, threadRun.Id, cancellationToken)).Value;

            if (threadRun.Status == RunStatus.RequiresAction && threadRun.RequiredAction is SubmitToolOutputsAction submitToolOutputsAction && toolCallFunc != null)
            {
                List<ToolOutput> toolOutputs = await toolCallFunc(submitToolOutputsAction.ToolCalls);
                threadRun = (await client.SubmitToolOutputsToRunAsync(threadRun, toolOutputs, cancellationToken)).Value;
            }
        }
        while (threadRun.Status == RunStatus.Queued || threadRun.Status == RunStatus.InProgress);

        //check for failed
        if (threadRun.Status == RunStatus.Failed)
        {
            logger.LogError("Assistant {AssistandId} Thread {ThreadId} ThreadRun {ThreadRunId} Failed. {Error}.", crOptions.AssistantId, threadId, threadRun.Id, threadRun.LastError.Message);
            throw new InvalidOperationException($"ThreadRun {threadRun.Id} failed {threadRun.LastError.Message}");
        }

        Response<PageableList<ThreadMessage>> afterRunMessagesResponse = await client.GetMessagesAsync(threadId, cancellationToken: cancellationToken);
        IReadOnlyList<ThreadMessage> messages = afterRunMessagesResponse.Value.Data;

        // messages iterate from newest to oldest, with the messages[0] being the most recent
        StringBuilder response = new();

        //get the most recent Assistant messages/content items for reponding to the user
        foreach (ThreadMessage threadMessage in messages.TakeWhile(m => m.Role == MessageRole.Assistant))
        {
            //Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
            foreach (MessageContent contentItem in threadMessage.ContentItems)
            {
                if (contentItem is MessageTextContent textItem)
                {
                    response.Append(textItem.Text);
                }
                else if (contentItem is MessageImageFileContent imageFileItem)
                {
                    response.Append($"<image from ID: {imageFileItem.FileId}");
                }
            }
        }

        return response.ToString();
    }
}
