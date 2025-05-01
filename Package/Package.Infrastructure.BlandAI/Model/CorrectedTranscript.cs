using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;
public class Aligned
{
    [JsonPropertyName("start")]
    public double? Start { get; set; }

    [JsonPropertyName("end")]
    public double? End { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("speaker")]
    public string? Speaker { get; set; }

    [JsonPropertyName("similarity")]
    public double? Similarity { get; set; }
}

public class Corrected
{
    [JsonPropertyName("start")]
    public double? Start { get; set; }

    [JsonPropertyName("end")]
    public double? End { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("speaker")]
    public int? Speaker { get; set; }
}

public class Original
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("c_id")]
    public string? CId { get; set; }
}

public class CorrectedTranscript
{
    [JsonPropertyName("corrected")]
    public List<Corrected> Corrected { get; set; } = null!;

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("aligned")]
    public List<Aligned> Aligned { get; set; } = null!;

    [JsonPropertyName("original")]
    public List<Original> Original { get; set; } = null!;
}
