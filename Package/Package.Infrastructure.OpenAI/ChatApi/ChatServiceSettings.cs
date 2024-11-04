namespace Package.Infrastructure.OpenAI.ChatApi;

public class ChatServiceSettings
{
    public const string ConfigSectionName = "OpenAIChatServiceSettings";
    public string Key { get; set; } = null!;
    public string Model { get; set; } = null!;
}
