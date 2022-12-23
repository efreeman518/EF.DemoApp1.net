using System.Diagnostics.CodeAnalysis;

namespace Application.Services;

[ExcludeFromCodeCoverage]
public class TodoServiceSettings
{
    public const string ConfigSectionName = "TodoServiceSettings";
    public string? StringValue { get; set; }
    public int? IntValue { get; set; }
}
