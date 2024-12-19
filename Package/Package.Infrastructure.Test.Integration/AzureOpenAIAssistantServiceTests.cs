using Azure.AI.OpenAI.Assistants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.AzureOpenAI.Assistant;
using Package.Infrastructure.Test.Integration.AzureOpenAI.Assistant;
using System.Text.Json;

namespace Package.Infrastructure.Test.Integration;

[Ignore("AzureOpenAI deployment required - https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md")]

// The Assistants feature area is in beta, with API specifics subject to change.
// Suppress the [Experimental] warning via .csproj or, as here, in the code to acknowledge.
// #pragma warning disable OPENAI001

[TestClass]
public class AzureOpenAIAssistantServiceTests : IntegrationTestBase
{
    readonly IAssistantService _assistantService;

    public AzureOpenAIAssistantServiceTests()
    {
        _assistantService = Services.GetRequiredService<ISomeAssistantService>();
    }

    [TestMethod]
    public async Task Conversation_pass()
    {
        var aOptions = new AssistantCreationOptions(Config.GetValue<string>("SomeAssistantSettings:DeploymentName"))
        {
            Name = "test-assistant",
            Description = "Data finding assistant",
            Instructions = "Helps users find info",
            Tools = { getWeather },
        };

        (var assistantId, var threadId) = await _assistantService.CreateAssistandAndThreadAsync(aOptions);

        var crOptions = new CreateRunOptions(assistantId);
        var response = await _assistantService.AddMessageAndRunThreadAsync(threadId, "weather in dallas", crOptions, RunToolCalls);
        Assert.IsNotNull(response);
    }

    private static async Task<List<ToolOutput>> RunToolCalls(IReadOnlyList<RequiredToolCall> toolCalls)
    {
        await Task.CompletedTask;

        var toolOutputs = new List<ToolOutput>();

        foreach (RequiredToolCall toolCall in toolCalls)
        {
            if (toolCall is RequiredFunctionToolCall functionToolCall)
            {
                using JsonDocument argumentsJson = JsonDocument.Parse(functionToolCall.Arguments);
                switch (functionToolCall.Name)
                {
                    case nameof(getWeather):
                        {
                            string location = argumentsJson.RootElement.GetProperty("location").GetString() ?? "San Diego, CA";
                            string? unit = (argumentsJson.RootElement.TryGetProperty("unit", out JsonElement unitElement))
                                ? unitElement.GetString()
                                : null;
                            toolOutputs.Add(new ToolOutput(functionToolCall, GetWeather(location, unit)));
                            break;
                        }
                }
            }
        }

        return toolOutputs;
    }

    static string GetWeather(string location, string? unit = "f")
    {
        //await some api
        return $"{location} temp is 70 {unit}";
    }

    readonly FunctionToolDefinition getWeather = new(
        name: nameof(getWeather),
        description: "Determine the weather for the given location.",
        parameters: BinaryData.FromBytes("""
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

}
