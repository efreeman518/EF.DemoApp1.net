using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Linq;

namespace SampleApp.Api.Swagger;

internal class SwaggerUIConfigurationOptions : IConfigureOptions<SwaggerUIOptions>
{
    private readonly IApiVersionDescriptionProvider provider;
    public SwaggerUIConfigurationOptions(IApiVersionDescriptionProvider provider)
    {
        this.provider = provider;
    }

    public void Configure(SwaggerUIOptions options)
    {
        provider.ApiVersionDescriptions.Select(desc => desc.GroupName).ToList().ForEach(groupName =>
            options.SwaggerEndpoint($"{groupName}/swagger.json", groupName.ToUpperInvariant())
        );
    }
}
