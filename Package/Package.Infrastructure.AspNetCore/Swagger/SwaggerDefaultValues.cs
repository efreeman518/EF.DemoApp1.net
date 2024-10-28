//using Microsoft.AspNetCore.Mvc.ApiExplorer;
//using Microsoft.OpenApi.Any;
//using Microsoft.OpenApi.Models;
//using Swashbuckle.AspNetCore.SwaggerGen;

//namespace Package.Infrastructure.AspNetCore.Swagger;

///// <summary>
///// https://markgossa.com/2022/05/asp-net-6-api-versioning-swagger.html
///// </summary>
//public class SwaggerDefaultValues : IOperationFilter
//{
//    public void Apply(OpenApiOperation operation, OperationFilterContext context)
//    {
//        var apiDescription = context.ApiDescription;

//        operation.Deprecated |= apiDescription.IsDeprecated();

//        if (operation.Parameters == null)
//        {
//            return;
//        }

//        foreach (var parameter in operation.Parameters)
//        {
//            var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

//            parameter.Description ??= description.ModelMetadata?.Description;

//            if (parameter.Schema.Default is null && description.DefaultValue is not null)
//            {
//                parameter.Schema.Default = new OpenApiString(description.DefaultValue.ToString());
//            }

//            parameter.Required |= description.IsRequired;
//        }
//    }
//}
