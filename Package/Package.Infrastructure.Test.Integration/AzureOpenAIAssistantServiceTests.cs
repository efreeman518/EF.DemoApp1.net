using Azure.AI.OpenAI.Assistants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.AzureOpenAI.Assistants;
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
    public async Task BasicWeatherConversation_pass()
    {
        var aOptions = new AssistantCreationOptions(Config.GetValue<string>("SomeAssistantSettings:DeploymentName"))
        {
            Name = "test-weather-assistant",
            Description = "Data finding assistant",
            Instructions = "Helps users find info",
            Tools = { getWeather },
        };

        var testAssistant = await _assistantService.GetOrCreateAssistantByName("test-weather-assistant", aOptions);
        var thread = await _assistantService.CreateThreadAsync();
        var response = await _assistantService.AddMessageAndRunThreadAsync(thread.Id, "weather in san diego, CA", new CreateRunOptions(testAssistant.Id), RunToolCallsWeatherTest);
        Assert.IsNotNull(response);
    }

    private static async Task<List<ToolOutput>> RunToolCallsWeatherTest(IReadOnlyList<RequiredToolCall> toolCalls)
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

    [TestMethod]
    public async Task DeleteAllAssistants_pass()
    {
        var keepers = new List<string> { "test-weather-assistant" };
        var response = await _assistantService.DeleteAssisantsAsync(keepers);
        Assert.IsTrue(response);
    }
}
