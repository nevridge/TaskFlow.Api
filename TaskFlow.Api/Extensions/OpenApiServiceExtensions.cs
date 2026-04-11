using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

namespace TaskFlow.Api.Extensions;

/// <summary>
/// Extension methods for configuring OpenAPI / Scalar documentation services.
/// </summary>
public static class OpenApiServiceExtensions
{
    /// <summary>
    /// Registers one Microsoft.AspNetCore.OpenApi document per API version.
    /// Document names match the group-name format configured in
    /// <see cref="ApiVersioningServiceExtensions"/> (e.g. "v1", "v2").
    /// </summary>
    public static IServiceCollection AddOpenApi(this IServiceCollection services)
    {
        // Register an OpenAPI document for each known API version.
        // Add a new services.AddOpenApi("v2", ...) entry when a v2 is introduced.
        services.AddOpenApi("v1", options =>
        {
            // Use a document transformer so we can reflect deprecation status at
            // runtime without needing to call BuildServiceProvider() here.
            options.AddDocumentTransformer<ApiVersionDocumentTransformer>();
        });

        return services;
    }

    /// <summary>
    /// Maps the OpenAPI JSON endpoints and the Scalar interactive UI.
    /// Call this inside the development-only block in Program.cs.
    /// </summary>
    public static WebApplication UseOpenApiWithScalar(this WebApplication app)
    {
        // Serve /openapi/{documentName}.json for every registered document.
        app.MapOpenApi();

        // Mount the Scalar interactive reference UI at /scalar/{documentName}.
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("TaskFlow API");
        });

        return app;
    }
}

/// <summary>
/// Document transformer that sets OpenAPI document metadata, picking up
/// deprecation status from <see cref="IApiVersionDescriptionProvider"/> when available.
/// </summary>
internal sealed class ApiVersionDocumentTransformer(
    IApiVersionDescriptionProvider? versionProvider = null)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        Microsoft.OpenApi.OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var groupName = context.DocumentName; // e.g. "v1"

        // Try to match against a discovered API version description.
        var versionDescription = versionProvider?.ApiVersionDescriptions
            .FirstOrDefault(d => d.GroupName == groupName);

        var version = versionDescription?.ApiVersion.ToString() ?? groupName;
        var isDeprecated = versionDescription?.IsDeprecated ?? false;

        document.Info = new()
        {
            Title = "TaskFlow API",
            Version = version,
            Description = isDeprecated
                ? $"TaskFlow API {version} — this version has been deprecated."
                : "A RESTful task management API demonstrating modern .NET practices."
        };

        return Task.CompletedTask;
    }
}
