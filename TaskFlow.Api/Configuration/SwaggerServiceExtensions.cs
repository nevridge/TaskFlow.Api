using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TaskFlow.Api.Configuration;

/// <summary>
/// Extension methods for configuring Swagger/OpenAPI services
/// </summary>
public static class SwaggerServiceExtensions
{
    /// <summary>
    /// Adds Swagger/OpenAPI services to the service collection with API versioning support
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        // Register Swagger configuration for API versioning
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

        services.AddSwaggerGen(options =>
        {
            // Add a custom operation filter to add version parameter in UI
            options.OperationFilter<SwaggerDefaultValues>();
        });

        return services;
    }
}

/// <summary>
/// Configures Swagger generation options for API versioning
/// </summary>
public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider = provider;

    public void Configure(SwaggerGenOptions options)
    {
        // Create a Swagger document for each discovered API version
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                new OpenApiInfo
                {
                    Title = "TaskFlow API",
                    Version = description.ApiVersion.ToString(),
                    Description = description.IsDeprecated
                        ? "This API version has been deprecated."
                        : "A RESTful task management API demonstrating modern .NET practices."
                });
        }
    }
}

/// <summary>
/// Adds default values to Swagger operation metadata
/// </summary>
public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(Microsoft.OpenApi.Models.OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        // Mark as deprecated if the API version is deprecated
        operation.Deprecated |= apiDescription.IsDeprecated();

        if (operation.Parameters == null)
        {
            return;
        }

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions
                .FirstOrDefault(p => p.Name == parameter.Name);

            if (description == null)
            {
                continue;
            }

            parameter.Description ??= description.ModelMetadata?.Description;

            if (parameter.Schema.Default == null && description.DefaultValue != null)
            {
                var defaultValueString = description.DefaultValue?.ToString();
                if (!string.IsNullOrEmpty(defaultValueString))
                {
                    parameter.Schema.Default = new Microsoft.OpenApi.Any.OpenApiString(defaultValueString);
                }
            }

            parameter.Required |= description.IsRequired;
        }
    }
}
