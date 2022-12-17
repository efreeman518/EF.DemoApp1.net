namespace Application.Services;

public class TodoServiceSettings
{
    public const string ConfigSectionName = "TodoServiceSettings";
    public string? StringValue { get; set; }
    public int? IntValue { get; set; }
}
