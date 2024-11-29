using OpenAI.Chat;
using Package.Infrastructure.Common.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Package.Infrastructure.AzureOpenAI;
public class ChatMessageJsonConverter : JsonConverter<ChatMessage>
{
    public override ChatMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonDocument = JsonDocument.ParseValue(ref reader);
        var root = jsonDocument.RootElement;
        var role = root.GetProperty("role").GetString() ?? throw new JsonException();
        string? toolCallId = root.TryGetProperty("toolCallId", out var elTool) ? elTool.GetString() : null;
        //var contentParts = JsonElementToArray(root, el => ChatMessageContentPart.CreateTextPart(el.GetProperty("contentParts").GetString()));//.RefusalChatMessageContentPart.CreateTextPart(el.GetProperty("contentParts").GetString())); 

        //AssistantChatMessage can have contentParts or toolCalls
        var content = root.GetProperty("content").GetString();
        List<ChatToolCall> toolCallsList = [];
        if (root.TryGetProperty("toolCalls", out var toolCalls))
        {

            using JsonDocument document = JsonDocument.Parse(toolCalls.GetString()!);
            JsonElement rootTools = document.RootElement;

            if (rootTools.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement el in rootTools.EnumerateArray())
                {
                    var parameters = BinaryData.FromString(el.GetProperty("FunctionArguments").ToString());
                    toolCallsList.Add(ChatToolCall.CreateFunctionToolCall(el.GetProperty("Id").GetString(), el.GetProperty("FunctionName").GetString(), parameters));
                }
            }
        }

        //var contentParts = new ChatMessageContentPart[] { ChatMessageContentPart.CreateTextPart(content) };
        ChatMessage chatMessage = role switch
        {
            "OpenAI.Chat.SystemChatMessage" => new SystemChatMessage(content),
            "OpenAI.Chat.UserChatMessage" => new UserChatMessage(content),
            "OpenAI.Chat.AssistantChatMessage" => !string.IsNullOrEmpty(content) ? new AssistantChatMessage(content) : new AssistantChatMessage(toolCallsList),
            "OpenAI.Chat.ToolChatMessage" => new ToolChatMessage(toolCallId, content),
            _ => throw new JsonException($"Invalid role {role}")
        };

        jsonDocument.Dispose();
        return chatMessage;
    }
    public override void Write(Utf8JsonWriter writer, ChatMessage value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("role", value.GetType().ToString());
        writer.WriteString("content", value.Content.Count > 0 ? value.Content[0].Text : null); //more than 1 content part not supported
        if (value is ToolChatMessage tool)
        {
            writer.WriteString("toolCallId", tool.ToolCallId);
        }
        if (value is AssistantChatMessage tool1 && tool1.ToolCalls.Count > 0)
        {
            writer.WriteString("toolCalls", tool1.ToolCalls.SerializeToJson());
        }
        writer.WriteEndObject();
    }

    public static T[] JsonElementToArray<T>(JsonElement jsonElement, Func<JsonElement, T> converter)
    {
        if (jsonElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("JsonElement is not an array.");

        return jsonElement.EnumerateArray()
                          .Select(converter)
                          .ToArray();
    }
}
