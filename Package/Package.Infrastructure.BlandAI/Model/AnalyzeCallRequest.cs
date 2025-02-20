using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;

public class AnalyzeCallRequest
{
    [JsonPropertyName("goal")]
    public string Goal { get; set; } = null!;

    /* 
       "questions": [
          ["Who answered the call?", "human or voicemail"],
          ["Positive feedback about the product: ", "string"],
          ["Negative feedback about the product: ", "string"],
          ["Customer confirmed they were satisfied", "boolean"]
        ]
    */
    [JsonPropertyName("questions")]
    public List<List<string>> Questions { get; set; } = null!;
}

public class AnalyzeCallResponse : DefaultResponse
{
    /* 
        "answers": [
            "human",
            "Customer found the product sturdy and reliable",
            "A bit heavy",
            true
        ]
     */
    [JsonPropertyName("answers")]
    public List<object> Answers { get; set; } = null!;

    [JsonPropertyName("credits_used")]
    public decimal CreditsUsed { get; set; } 
}
