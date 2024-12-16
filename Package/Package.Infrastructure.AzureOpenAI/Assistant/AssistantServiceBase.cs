using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Assistants;
using OpenAI.Chat;

namespace Package.Infrastructure.AzureOpenAI.Assistant;

/// <summary>
/// Assistants are stateful, more features than chat
/// https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md#use-assistants-and-stream-a-run
/// https://platform.openai.com/docs/assistants/overview
/// </summary>
/// <param name="logger"></param>
/// <param name="settings"></param>
/// <param name="openAIclient"></param>
public abstract class AssistantServiceBase(ILogger<AssistantServiceBase> logger, IOptions<AssistantServiceSettingsBase> settings,
    AzureOpenAIClient openAIclient) : IAssistantService
{

    // The Assistants feature area is in beta, with API specifics subject to change.
    // Suppress the [Experimental] warning via .csproj or, as here, in the code to acknowledge.
#pragma warning disable OPENAI001
    private readonly AssistantClient assistantClient = openAIclient.GetAssistantClient();

    public async Task<string> RunAsync(string assistantName, string? threadId, AssistantCreationOptions? options = null,
        Func<List<ChatMessage>, IReadOnlyList<ToolDefinition>, Task>? toolCallFunc = null, int? maxCompletionMessageCount = null, int maxToolCallRounds = 5,
        CancellationToken cancellationToken = default)
    {
        var aOptions = new AssistantCreationOptions
        { 
            Name = assistantName,
            Description = "Job search assistant",
            Instructions = "Helps users find jobs",
            ResponseFormat = AssistantResponseFormat.CreateTextFormat(),
            NucleusSamplingFactor = 0.1F,
              
        };

        var assistant = await assistantClient.CreateAssistantAsync(settings.Value.DeploymentName);
        return "";
    }
}
