using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.IO;
using System.Reflection;

namespace SampleApp.Api.Swagger;

/// <summary>
/// https://markgossa.com/2022/05/asp-net-6-api-versioning-swagger.html
/// </summary>
internal class SwaggerGenConfigurationOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider provider;
    public SwaggerGenConfigurationOptions(IApiVersionDescriptionProvider provider)
    {
        this.provider = provider;
    }
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                new OpenApiInfo
                {
                    Title = "Sample API",
                    Version = description.ApiVersion.ToString()
                });
        }
    }

    public static void AddSwaggerXmlComments(SwaggerGenOptions o)
    {
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    }
}
