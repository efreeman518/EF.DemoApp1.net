using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Package.Infrastructure.AspNetCore.Swagger;

public class SwaggerUIConfigurationOptions : IConfigureOptions<SwaggerUIOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public SwaggerUIConfigurationOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerUIOptions options)
    {
        _provider.ApiVersionDescriptions.Select(desc => desc.GroupName).ToList().ForEach(groupName =>
            options.SwaggerEndpoint($"{groupName}/swagger.json", groupName.ToUpperInvariant())
        );
    }
}
