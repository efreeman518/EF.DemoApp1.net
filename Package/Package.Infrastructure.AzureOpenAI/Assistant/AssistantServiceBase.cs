using Azure;
using Azure.AI.OpenAI.Assistants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Package.Infrastructure.AzureOpenAI.Assistant;

// The Assistants feature area is in beta, with API specifics subject to change.
// Suppress the [Experimental] warning via .csproj or, as here, in the code to acknowledge.
#pragma warning disable OPENAI001

/// <summary>
/// Assistants are stateful, more features than chat
/// https://www.nuget.org/packages/Azure.AI.OpenAI.Assistants/1.0.0-beta.4#show-readme-container
/// https://github.com/Azure/azure-sdk-for-net/blob/Azure.AI.OpenAI.Assistants_1.0.0-beta.4/sdk/openai/Azure.AI.OpenAI.Assistants/README.md
/// https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md#use-assistants-and-stream-a-run
/// https://platform.openai.com/docs/assistants/overview
/// </summary>
/// <param name="logger"></param>
/// <param name="settings"></param>
/// <param name="openAIclient"></param>
public abstract class AssistantServiceBase(ILogger<AssistantServiceBase> logger, IOptions<AssistantServiceSettingsBase> settings, AssistantsClient client) : IAssistantService
{
    //Experimental AssistantsClient (client factory does not currently support this client)
    //private readonly AssistantClient assistantClient = clientFactory.CreateClient(settings.Value.ResourceName).GetAssistantClient();

    public async Task<(string, string)> CreateAssistandAndThreadAsync(string initMessage, 
        AssistantCreationOptions? aOptions = null, AssistantThreadCreationOptions? tOptions = null, CancellationToken cancellationToken = default)
    {
        //why is this needed?
        Azure.AI.OpenAI.Assistants.Assistant assistant = (await client.CreateAssistantAsync(aOptions, cancellationToken)).Value;
        AssistantThread thread = (await client.CreateThreadAsync(tOptions, cancellationToken)).Value;
        return (assistant.Id, thread.Id);
    }

    public async Task<string> AddMessageAndRunThreadAsync(string threadId, string userMessage, CreateRunOptions? options = null,
        Func<IReadOnlyList<RequiredToolCall>, Task<List<ToolOutput>>>? toolCallFunc = null, CancellationToken cancellationToken = default)
    {
        ThreadMessage message = (await client.CreateMessageAsync(threadId, MessageRole.User, userMessage, cancellationToken: cancellationToken)).Value;
        Response<ThreadRun> runResponse = await client.CreateRunAsync(threadId, options, cancellationToken);
        ThreadRun threadRun = runResponse.Value;

        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            threadRun = (await client.GetRunAsync(threadId, threadRun.Id, cancellationToken)).Value;

            if (threadRun.Status == RunStatus.RequiresAction && runResponse.Value.RequiredAction is SubmitToolOutputsAction submitToolOutputsAction && toolCallFunc != null)
            {
                List<ToolOutput> toolOutputs = await toolCallFunc(submitToolOutputsAction.ToolCalls);// new();
                //foreach (RequiredToolCall toolCall in submitToolOutputsAction.ToolCalls)
                //{
                //    toolOutputs.Add(GetResolvedToolOutput(toolCall));
                //}
                runResponse = await client.SubmitToolOutputsToRunAsync(threadRun, toolOutputs, cancellationToken);
            }
        }
        while (threadRun.Status == RunStatus.Queued || runResponse.Value.Status == RunStatus.InProgress);

        Response<PageableList<ThreadMessage>> afterRunMessagesResponse = await client.GetMessagesAsync(threadId, cancellationToken: cancellationToken);
        IReadOnlyList<ThreadMessage> messages = afterRunMessagesResponse.Value.Data;

        // Note: messages iterate from newest to oldest, with the messages[0] being the most recent
        StringBuilder response = new();
        foreach (ThreadMessage threadMessage in messages)
        {
            Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
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
