using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Common.Swagger;

/// <summary>Ensures all query parameter names are camelCase in the OpenAPI spec.</summary>
public sealed class CamelCaseQueryParameterFilter : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        if (parameter.In != ParameterLocation.Query || parameter.Name.Length == 0)
            return;

        parameter.Name = char.ToLowerInvariant(parameter.Name[0]) + parameter.Name[1..];
        if (parameter.Schema is not null && string.IsNullOrEmpty(parameter.Schema.Title))
            parameter.Schema.Title = parameter.Name;
    }
}
