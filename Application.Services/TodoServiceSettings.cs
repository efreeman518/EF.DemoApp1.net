using System.Diagnostics.CodeAnalysis;

namespace Application.Services;

[ExcludeFromCodeCoverage]
public class TodoServiceSettings
{
    public const string ConfigSectionName = "TodoServiceSettings";
    public string? StringProperty { get; set; }
    public int? IntProperty { get; set; }
}
