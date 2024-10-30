using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace Package.Infrastructure.OpenAI.ChatApi;

//https://github.com/openai/openai-dotnet?tab=readme-ov-file
//https://platform.openai.com/settings/organization/usage
//https://platform.openai.com/docs/models

public class ChatService : IChatService
{
    private readonly ChatClient chatClient;

    public ChatService(IOptions<ChatServiceSettings> settings)
    {
        var openAIclient = new OpenAIClient(settings.Value.Key);
        chatClient = openAIclient.GetChatClient(settings.Value.Model);
    }

    public async Task<List<string>> ChatStream(Request request)
    {
        List<string> response = [];

        AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates = chatClient.CompleteChatStreamingAsync(request.Prompt);

        await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
        {
            if (completionUpdate.ContentUpdate.Count > 0)
            {
                response.Add(completionUpdate.ContentUpdate[0].Text);
            }
        }

        return response;
    }

    public async Task<string> ChatCompletion(Request request)
    {
        ChatCompletion completion = await chatClient.CompleteChatAsync(request.Prompt);
        return completion.Content[0].Text;
    }




    private static string GetCurrentLocation()
    {
        // Call the location API here.
        return "San Francisco";
    }

    private static string GetCurrentWeather(string location, string unit = "celsius")
    {
        // Call the weather API here.
        _ = location.GetHashCode();
        return $"31 {unit}";
    }

    private static readonly ChatTool getCurrentLocationTool = ChatTool.CreateFunctionTool(
        functionName: nameof(GetCurrentLocation),
        functionDescription: "Get the user's current location"
    );

    private static readonly ChatTool getCurrentWeatherTool = ChatTool.CreateFunctionTool(
        functionName: nameof(GetCurrentWeather),
        functionDescription: "Get the current weather in a given location",
        functionParameters: BinaryData.FromBytes("""
        {
            "type": "object",
            "properties": {
                "location": {
                    "type": "string",
                    "description": "The city and state, e.g. Boston, MA"
                },
                "unit": {
                    "type": "string",
                    "enum": [ "celsius", "fahrenheit" ],
                    "description": "The temperature unit to use. Infer this from the specified location."
                }
            },
            "required": [ "location" ]
        }
        """u8.ToArray())
    );

    public async Task<string> ChatCompletionWithTools(Request request)
    {
        //ChatCompletion completion = await chatClient.CompleteChatAsync(request.Prompt, tools: new[] { getCurrentLocationTool, getCurrentWeatherTool });
        //return completion.Content[0].Text;

        List<ChatMessage> messages =
        [
            new UserChatMessage(request.Prompt) //("What's the weather like today?"),
        ];

        ChatCompletionOptions options = new()
        {
            Tools = { getCurrentLocationTool, getCurrentWeatherTool },
        };

        string response = "";
        bool requiresAction;

        do
        {
            requiresAction = false;
            ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);

            switch (completion.FinishReason)
            {
                case ChatFinishReason.Stop:
                    {
                        // Add the assistant message to the conversation history.
                        messages.Add(new AssistantChatMessage(completion));

                        response = completion.Content[0].Text;

                        break;
                    }

                case ChatFinishReason.ToolCalls:
                    {
                        // First, add the assistant message with tool calls to the conversation history.
                        messages.Add(new AssistantChatMessage(completion));

                        // Then, add a new tool message for each tool call that is resolved.
                        foreach (ChatToolCall toolCall in completion.ToolCalls)
                        {
                            switch (toolCall.FunctionName)
                            {
                                case nameof(GetCurrentLocation):
                                    {
                                        string toolResult = GetCurrentLocation();
                                        messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                                        break;
                                    }

                                case nameof(GetCurrentWeather):
                                    {
                                        // The arguments that the model wants to use to call the function are specified as a
                                        // stringified JSON object based on the schema defined in the tool definition. Note that
                                        // the model may hallucinate arguments too. Consequently, it is important to do the
                                        // appropriate parsing and validation before calling the function.
                                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                        bool hasLocation = argumentsJson.RootElement.TryGetProperty("location", out JsonElement location);
                                        bool hasUnit = argumentsJson.RootElement.TryGetProperty("unit", out JsonElement unit);

                                        if (!hasLocation)
                                        {
                                            throw new ArgumentNullException(nameof(location), "The location argument is required.");
                                        }

                                        string toolResult = hasUnit
                                            ? GetCurrentWeather(location!.GetString()!, unit!.GetString()!)
                                            : GetCurrentWeather(location!.GetString()!);
                                        messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                                        break;
                                    }

                                default:
                                    {
                                        // Handle other unexpected calls.
                                        throw new NotImplementedException();
                                    }
                            }
                        }

                        requiresAction = true;
                        break;
                    }

                case ChatFinishReason.Length:
                    throw new NotImplementedException("Incomplete model output due to MaxTokens parameter or token limit exceeded.");

                case ChatFinishReason.ContentFilter:
                    throw new NotImplementedException("Omitted content due to a content filter flag.");

                case ChatFinishReason.FunctionCall:
                    throw new NotImplementedException("Deprecated in favor of tool calls.");

                default:
                    throw new NotImplementedException(completion.FinishReason.ToString());
            }
        } while (requiresAction);

        return response;
    }
}

