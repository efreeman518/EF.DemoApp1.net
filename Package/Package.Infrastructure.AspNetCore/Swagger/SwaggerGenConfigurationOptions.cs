using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

namespace Package.Infrastructure.AspNetCore.Swagger;

//https://github.com/dotnet/aspnet-api-versioning/blob/1226167cc3e0796d82d49a4769614a0ef1e666b4/examples/AspNetCore/WebApi/MinimalOpenApiExample/ConfigureSwaggerOptions.cs

/// <summary>
/// Configures the Swagger generation options.
/// </summary>
/// <remarks>This allows API versioning to define a Swagger document per API version after the
/// <see cref="IApiVersionDescriptionProvider"/> service has been resolved from the service container.</remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="SwaggerGenConfigurationOptions"/> class.
/// </remarks>
/// <param name="provider">The <see cref="IApiVersionDescriptionProvider">provider</see> used to generate Swagger documents.</param>
public class SwaggerGenConfigurationOptions(IApiVersionDescriptionProvider provider, IOptions<SwaggerSettings> settings) : IConfigureOptions<SwaggerGenOptions>
{
    /// <inheritdoc />
    public void Configure(SwaggerGenOptions options)
    {
        // add a swagger document for each discovered API version
        // note: you might choose to skip or document deprecated API versions differently
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }
    }

    private OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var text = new StringBuilder($"{settings.Value.OpenApiTitle} {description.GroupName} OpenApi documentation.");
        var info = new OpenApiInfo()
        {
            Title = settings.Value.OpenApiTitle,
            Version = description.ApiVersion.ToString()
            //Contact = new OpenApiContact() { Name = "First Last", Email = "firstlast@somewhere.com" },
            //License = new OpenApiLicense() { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
        };

        if (description.IsDeprecated)
        {
            text.Append(" This API version has been deprecated.");
        }

        if (description.SunsetPolicy is SunsetPolicy policy)
        {
            if (policy.Date is DateTimeOffset when)
            {
                text.Append(" The API will be sunset on ")
                    .Append(when.Date.ToShortDateString())
                    .Append('.');
            }

            if (policy.HasLinks)
            {
                text.AppendLine();

                for (var i = 0; i < policy.Links.Count; i++)
                {
                    var link = policy.Links[i];

                    if (link.Type == "text/html")
                    {
                        text.AppendLine();

                        if (link.Title.HasValue)
                        {
                            text.Append(link.Title.Value).Append(": ");
                        }

                        text.Append(link.LinkTarget.OriginalString);
                    }
                }
            }
        }

        info.Description = text.ToString();

        return info;
    }

    public static void AddSwaggerXmlComments(SwaggerGenOptions o, string xmlCommentsFileName)
    {
        o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlCommentsFileName));
    }
}
