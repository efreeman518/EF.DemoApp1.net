using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Assistants;
using OpenAI.Files;
using System.ClientModel;
using System.Text;

namespace Package.Infrastructure.AzureOpenAI.Assistants;

//NONE OF THIS WORKS - "EXPERIMENTAL" = lack of docs, limited & conflicting sample code, incompatibility with default models, non-functioning hacks, breaking changes across beta versions, community complaints - lack of support

// The Assistants feature area is in beta, with API specifics subject to change.
// Suppress the [Experimental] warning via .csproj or, as here, in the code to acknowledge.
#pragma warning disable OPENAI001

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
/// <param name="assistantClient"></param>
public abstract class AssistantServiceBase(ILogger<AssistantServiceBase> logger, IOptions<AssistantServiceSettingsBase> settings,
    AzureOpenAIClient aoaiClient) : IAssistantService
{
    //Azure client factory does not currently support this AssistantsClient
    private readonly OpenAIFileClient fileClient = aoaiClient.GetOpenAIFileClient();
    private readonly AssistantClient assistantClient = aoaiClient.GetAssistantClient();

    //assistant
    public AsyncCollectionResult<Assistant> GetAssistantsAsync(AssistantCollectionOptions? options = null, CancellationToken cancellationToken = default) =>
        assistantClient.GetAssistantsAsync(options, cancellationToken);
    public async Task<Assistant> GetAssistantAsync(string assistantId, CancellationToken cancellationToken = default) =>
        (await assistantClient.GetAssistantAsync(assistantId, cancellationToken)).Value;
    public async Task<Assistant> CreateAssistantAsync(string model, AssistantCreationOptions? options = null, CancellationToken cancellationToken = default) =>
        (await assistantClient.CreateAssistantAsync(model, options, cancellationToken)).Value;
    public async Task<Assistant> ModifyAssistantAsync(string assistantId, AssistantModificationOptions options, CancellationToken cancellationToken = default) =>
        (await assistantClient.ModifyAssistantAsync(assistantId, options, cancellationToken)).Value;
    public async Task<bool> DeleteAssistantAsync(string assistantId, CancellationToken cancellationToken = default) =>
        (await assistantClient.DeleteAssistantAsync(assistantId, cancellationToken)).Value.Deleted;

    //threads
    public async Task<AssistantThread> GetThread(string threadId, CancellationToken cancellationToken = default) =>
        (await assistantClient.GetThreadAsync(threadId, cancellationToken)).Value;
    public async Task<AssistantThread> CreateThreadAsync(ThreadCreationOptions? options = null, CancellationToken cancellationToken = default) =>
        (await assistantClient.CreateThreadAsync(options, cancellationToken)).Value;


    public async Task<ThreadRun> CreateThreadAndRunAsync(string assistantId, ThreadCreationOptions? tOptions = null, RunCreationOptions? rOptions = null, CancellationToken cancellationToken = default) =>
        (await assistantClient.CreateThreadAndRunAsync(assistantId, tOptions, rOptions, cancellationToken)).Value;
    public async Task<AssistantThread> UpdateThreadAsync(string threadId, ThreadModificationOptions? options = null, CancellationToken cancellationToken = default) =>
        (await assistantClient.ModifyThreadAsync(threadId, options, cancellationToken)).Value;
    public async Task<bool> DeleteThreadAsync(string threadId, CancellationToken cancellationToken = default) =>
        (await assistantClient.DeleteThreadAsync(threadId, cancellationToken)).Value.Deleted;

    //messages
    public AsyncCollectionResult<ThreadMessage> GetMessagesAsync(string threadId, MessageCollectionOptions? options = null, CancellationToken cancellationToken = default) =>
        assistantClient.GetMessagesAsync(threadId, options, cancellationToken);
    public async Task<ThreadMessage> GetMessageAsync(string threadId, string messageId, CancellationToken cancellationToken = default) =>
        (await assistantClient.GetMessageAsync(threadId, messageId, cancellationToken)).Value;
    public async Task<ThreadMessage> CreateMessageAsync(string threadId, MessageRole role, IEnumerable<MessageContent> content, CancellationToken cancellationToken = default) =>
        (await assistantClient.CreateMessageAsync(threadId, role, content, cancellationToken: cancellationToken)).Value;
    public async Task<ThreadMessage> UpdateMessage(string threadId, string messageId, MessageModificationOptions options, CancellationToken cancellationToken = default) =>
        (await assistantClient.ModifyMessageAsync(threadId, messageId, options, cancellationToken)).Value;

    //runs
    public AsyncCollectionResult<ThreadRun> GetRunsAsync(string threadId, RunCollectionOptions? options = null, CancellationToken cancellationToken = default) =>
        assistantClient.GetRunsAsync(threadId, options, cancellationToken);
    public async Task<ThreadRun> GetRunAsync(string threadId, string runId, CancellationToken cancellationToken = default) =>
        (await assistantClient.GetRunAsync(threadId, runId, cancellationToken)).Value;
    public async Task<ThreadRun> CreateRunAsync(string threadId, string assistantId, RunCreationOptions options, CancellationToken cancellationToken = default) =>
        (await assistantClient.CreateRunAsync(threadId, assistantId, options, cancellationToken)).Value;
    public async Task<ThreadRun> CancelRunAsync(string threadId, string runId, CancellationToken cancellationToken = default) =>
        (await assistantClient.CancelRunAsync(threadId, runId, cancellationToken)).Value;
    public async Task<ThreadRun> SubmitToolOutputsToRunAsync(string threadId, string runId, IEnumerable<ToolOutput> toolOutputs, CancellationToken cancellationToken = default) =>
        (await assistantClient.SubmitToolOutputsToRunAsync(threadId, runId, toolOutputs, cancellationToken)).Value;


    //RunSteps
    public AsyncCollectionResult<RunStep> GetRunStepsRunAsync(string threadId, string runId, RunStepCollectionOptions? options = null, CancellationToken cancellationToken = default) =>
        assistantClient.GetRunStepsAsync(threadId, runId, options, cancellationToken);
    public async Task<RunStep> GetRunStepAsync(string threadId, string runId, string stepId, CancellationToken cancellationToken = default) =>
        (await assistantClient.GetRunStepAsync(threadId, runId, stepId, cancellationToken)).Value;

    public async Task<Assistant> GetOrCreateAssistantByName(string? assistantName = null, AssistantCreationOptions? options = null, CancellationToken cancellationToken = default)
    {
        Assistant? assistant = null;

        if (assistantName != null)
        {
            assistant = await GetAssistantByNameAsync(assistantName, cancellationToken);
        }

        assistant ??= await CreateAssistantAsync(settings.Value.Model, options, cancellationToken);

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
        var options = new AssistantCollectionOptions { PageSizeLimit = 100 };
        var assistants = GetAssistantsAsync(options, cancellationToken: cancellationToken);
        await foreach (var assistant in assistants)
        {
            if (assistant.Name == name)
            {
                return assistant;
            }
        }
        return null; // throw new InvalidOperationException($"Assistant with name {name} not found.");
    }

    public async Task<string?> GetFileIdByFilenameAsync(string filename, CancellationToken cancellationToken)
    {
        //var files = await GetFilesAsync(OpenAIFilePurpose.Assistants, cancellationToken: cancellationToken);
        var files = (await fileClient.GetFilesAsync(FilePurpose.Assistants, cancellationToken: cancellationToken)).Value;
        return files?.FirstOrDefault(f => f.Filename == filename)?.Id; // ?? throw new InvalidOperationException($"File with name {name} not found.");
    }

    public async Task<string?> UploadFileAsync(Stream fileStream, string filename, CancellationToken cancellationToken)
    {
        var file = (await fileClient.UploadFileAsync(fileStream, filename, FileUploadPurpose.Assistants, cancellationToken: cancellationToken)).Value;
        return file.Id;
    }

    public async Task<bool> DeleteAssistantsAsync(List<string>? keepers = null, CancellationToken cancellationToken = default)
    {
        var options = new AssistantCollectionOptions { PageSizeLimit = 100 };
        var assistants = GetAssistantsAsync(options, cancellationToken: cancellationToken);
        await foreach (var assistant in assistants)
        {
            if (keepers == null || !(keepers.Contains(assistant.Id) || keepers.Contains(assistant.Name)))
            {
                await DeleteAssistantAsync(assistant.Id, cancellationToken);
            }
        }
        return true;
    }

    public async Task<string> AddMessageAndRunThreadAsync(string assistantId, string threadId, string message, MessageCreationOptions? mOptions = null, RunCreationOptions? rOptions = null,
        Func<IReadOnlyList<RequiredAction>, Task<List<ToolOutput>>>? toolCallFunc = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<MessageContent> content = [MessageContent.FromText(message)];
        _ = await assistantClient.CreateMessageAsync(threadId, MessageRole.User, content, mOptions, cancellationToken: cancellationToken);
        ThreadRun threadRun = (await assistantClient.CreateRunAsync(threadId, assistantId, rOptions, cancellationToken)).Value;


        logger.LogInformation("Assistant {AssistandId} Thread {ThreadId} ThreadMessage created.", assistantId, threadId);

        //poll the thread run (and process tool calls) until it's in an end state
        //do
        //{
        //    await Task.Delay(TimeSpan.FromMilliseconds(settings.Value.RunThreadPollingDelayMilliseconds), cancellationToken);
        //    threadRun = (await assistantClient.GetRunAsync(threadId, threadRun.Id, cancellationToken)).Value;

        //    if (threadRun.Status == RunStatus.RequiresAction && toolCallFunc != null)
        //    {
        //        var tools = threadRun.RequiredActions.Where(ra => ra.ToolCallId != null).ToList();
        //        List<ToolOutput> toolOutputs = await toolCallFunc(tools);
        //        threadRun = (await assistantClient.SubmitToolOutputsToRunAsync(threadId, threadRun.Id, toolOutputs, cancellationToken)).Value;
        //    }
        //}
        //while (threadRun.Status == RunStatus.Queued || threadRun.Status == RunStatus.InProgress);

        StringBuilder response = new();
        var options = new RunCreationOptions();
        await foreach (StreamingUpdate update in assistantClient.CreateRunStreamingAsync(threadId, assistantId, options, cancellationToken))
        {
            if (update is RequiredActionUpdate updateAction && toolCallFunc != null) //update.UpdateKind == StreamingUpdateReason.RunRequiresAction &&
            {
                threadRun = updateAction.GetThreadRun();
                var tools = threadRun.RequiredActions.Where(ra => ra.ToolCallId != null).ToList();
                List<ToolOutput> toolOutputs = await toolCallFunc(tools);
                await assistantClient.SubmitToolOutputsToRunAsync(threadId, threadRun.Id, toolOutputs, cancellationToken);
            }
            else if (update is MessageContentUpdate messageContentUpdate)
            {
                //do something with the message content
                response.Append(messageContentUpdate.Text);
            }
        }

        //check for failed
        if (threadRun.Status == RunStatus.Failed)
        {
            logger.LogError("Assistant {AssistandId} Thread {ThreadId} ThreadRun {ThreadRunId} Failed. {Error}.", assistantId, threadId, threadRun.Id, threadRun.LastError.Message);
            throw new InvalidOperationException($"ThreadRun {threadRun.Id} failed {threadRun.LastError.Message}");
        }

        return response.ToString();
    }
}

#pragma warning restore OPENAI001