using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;

public class TokenResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}
