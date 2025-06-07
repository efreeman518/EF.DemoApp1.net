﻿using System.Text.Json.Serialization;

namespace Package.Infrastructure.BlandAI.Model;

public abstract record DefaultResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
