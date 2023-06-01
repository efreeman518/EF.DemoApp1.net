using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Package.Infrastructure.AspNetCore.Swagger;

/// <summary>
/// https://markgossa.com/2022/05/asp-net-6-api-versioning-swagger.html
/// </summary>
public class SwaggerGenConfigurationOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;
    private readonly SwaggerSettings _settings;

    public SwaggerGenConfigurationOptions(IApiVersionDescriptionProvider provider, IOptions<SwaggerSettings> settings)
    {
        _provider = provider;
        _settings = settings.Value;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                new OpenApiInfo
                {
                    Title = _settings.OpenApiTitle,
                    Version = description.ApiVersion.ToString()
                });
        }
    }

    public static void AddSwaggerXmlComments(SwaggerGenOptions o, string xmlCommentsFileName)
    {
        o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlCommentsFileName));
    }
}
