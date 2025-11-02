using Microsoft.OpenApi.Models;

namespace TaskFlow.Api.Configuration;

/// <summary>
/// Extension methods for configuring Swagger/OpenAPI services
/// </summary>
public static class SwaggerServiceExtensions
{
    /// <summary>
    /// Adds Swagger/OpenAPI services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskFlow API", Version = "v1" });
        });

        return services;
    }
}
