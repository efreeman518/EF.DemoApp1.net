using OpenAI.Chat;
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
        var contentParts = new ChatMessageContentPart[] { ChatMessageContentPart.CreateTextPart(root.GetProperty("content").GetString()) };
        ChatMessage chatMessage = role switch
        {
            "OpenAI.Chat.SystemChatMessage" => new SystemChatMessage(contentParts),
            "OpenAI.Chat.UserChatMessage" => new UserChatMessage(contentParts),
            "OpenAI.Chat.AssistantChatMessage" => new AssistantChatMessage(contentParts),
            "OpenAI.Chat.ToolChatMessage" => new ToolChatMessage(toolCallId, contentParts),
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
