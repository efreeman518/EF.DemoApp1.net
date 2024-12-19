using Azure;
using Azure.AI.OpenAI.Assistants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Package.Infrastructure.AzureOpenAI.Assistant;

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
    //private readonly AssistantClient assistantClient = clientFactory.CreateClient(settings.Value.ResourceName).GetAssistantClient();

    public async Task<(string, string)> CreateAssistandAndThreadAsync(AssistantCreationOptions? aOptions = null, AssistantThreadCreationOptions? tOptions = null, CancellationToken cancellationToken = default)
    {
        Azure.AI.OpenAI.Assistants.Assistant assistant = (await client.CreateAssistantAsync(aOptions, cancellationToken)).Value;

        AssistantThread thread = tOptions == null
            ? (await client.CreateThreadAsync(cancellationToken)).Value
            : (await client.CreateThreadAsync(tOptions, cancellationToken)).Value;

        logger.LogInformation("Assistant created: {AssistantId}, Thread created: {ThreadId}", assistant.Id, thread.Id);
        return (assistant.Id, thread.Id);
    }

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
