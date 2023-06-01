namespace Package.Infrastructure.OpenAI.ChatApi;

public class ChatServiceSettings
{
    public const string ConfigSectionName = "ChatServiceSettings";
    public string Key { get; set; } = null!;
    public string Model { get; set; } = null!;
}
