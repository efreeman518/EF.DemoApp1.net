namespace Application.Services;

public class TodoServiceSettings
{
    public static string ConfigSectionName = "TodoServiceSettings";
    public string? StringValue { get; set; }
    public int? IntValue { get; set; }
}
