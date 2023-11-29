namespace Package.Infrastructure.AspNetCore.Swagger;

public class SwaggerSettings
{
    public const string ConfigSectionName = "SwaggerSettings";
    public string? XmlCommentsPath { get; set; }
    public string OpenApiTitle { get; set; } = "Service";
}
