using Microsoft.Extensions.DependencyInjection;
using OpenAI.Assistants;
using Package.Infrastructure.AzureOpenAI.Assistant;
using Package.Infrastructure.Test.Integration.AzureOpenAI.Assistant;

namespace Package.Infrastructure.Test.Integration;

//[Ignore("AzureOpenAI deployment required - https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md")]

// The Assistants feature area is in beta, with API specifics subject to change.
// Suppress the [Experimental] warning via .csproj or, as here, in the code to acknowledge.
#pragma warning disable OPENAI001

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
        var aOptions = new AssistantCreationOptions
        {
            Name = "test-assistant",
            Description = "Data finding assistant",
            Instructions = "Helps users find info",
            ResponseFormat = AssistantResponseFormat.CreateTextFormat(),
            NucleusSamplingFactor = 0.1F,
            Tools = { getWeather }
        };

        (var assistantId, var threadId) = await _assistantService.CreateAssistandAndThreadAsync("why is the sky blue", aOptions);
        var response = await _assistantService.RunAsync(assistantId, threadId);
        Assert.IsNotNull(response);
    }


    private static string GetWeather(string location, string unit = "celsius")
    {
        // Call the weather API here.
        return $"{location} temp is 70 {unit}";
    }

    private readonly ToolDefinition getWeather = ToolDefinition.CreateFunction(
        name: nameof(GetWeather),
        description: "Determine the weather for the given location and date.",
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
        """u8.ToArray()),
            strictParameterSchemaEnabled: true
        );

}
