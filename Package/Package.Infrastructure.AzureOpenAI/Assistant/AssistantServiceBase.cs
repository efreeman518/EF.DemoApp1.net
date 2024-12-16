using Azure.AI.OpenAI;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Assistants;
using System.Text;

namespace Package.Infrastructure.AzureOpenAI.Assistant;

// The Assistants feature area is in beta, with API specifics subject to change.
// Suppress the [Experimental] warning via .csproj or, as here, in the code to acknowledge.
#pragma warning disable OPENAI001

/// <summary>
/// Assistants are stateful, more features than chat
/// https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md#use-assistants-and-stream-a-run
/// https://platform.openai.com/docs/assistants/overview
/// </summary>
/// <param name="logger"></param>
/// <param name="settings"></param>
/// <param name="openAIclient"></param>
public abstract class AssistantServiceBase(ILogger<AssistantServiceBase> logger, IOptions<AssistantServiceSettingsBase> settings,
    IAzureClientFactory<AzureOpenAIClient> clientFactory) : IAssistantService
{
    private readonly AzureOpenAIClient openAIclient = clientFactory.CreateClient("AOAIAssistant");

    public async Task<(string, string)> CreateAssistandAndThreadAsync(string initMessage, AssistantCreationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var assistantClient = openAIclient.GetAssistantClient();
        var clientResultAssistant = await assistantClient.CreateAssistantAsync(settings.Value.DeploymentName, options, cancellationToken);

        ThreadInitializationMessage threadInitMessage = new(MessageRole.User,
            [
                initMessage
            ]);
        AssistantThread thread = await assistantClient.CreateThreadAsync(new ThreadCreationOptions()
        {
            InitialMessages = { threadInitMessage }
        }, cancellationToken);

        return(clientResultAssistant.Value.Id, thread.Id);
    }

    public async Task<string> RunAsync(string assistantId, string threadId, RunCreationOptions? options = null,
        Func<IReadOnlyList<ToolDefinition>, Task>? toolCallFunc = null, CancellationToken cancellationToken = default)
    {
        var assistantClient = openAIclient.GetAssistantClient();

        var response = new StringBuilder();
        await foreach (StreamingUpdate streamingUpdate in assistantClient.CreateRunStreamingAsync(threadId, assistantId, options, cancellationToken))
        {
            if (streamingUpdate.UpdateKind == StreamingUpdateReason.RunCreated)
            {
                Console.WriteLine($"--- Run started! ---");
            }
            else if (streamingUpdate is MessageContentUpdate contentUpdate)
            {
                response.Append(contentUpdate.Text);
                //Console.Write(contentUpdate.Text);
                //if (contentUpdate.ImageFileId is not null)
                //{
                //    Console.WriteLine($"[Image content file ID: {contentUpdate.ImageFileId}");
                //}
            }
        }

        return response.ToString();
    }
}
