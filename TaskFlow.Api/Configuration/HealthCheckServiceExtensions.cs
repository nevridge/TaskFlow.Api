using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskFlow.Api.Data;

namespace TaskFlow.Api.Configuration;

/// <summary>
/// Extension methods for configuring health check services
/// </summary>
public static class HealthCheckServiceExtensions
{
    /// <summary>
    /// Adds health check services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        // Add health checks with database connectivity validation
        services.AddHealthChecks()
            .AddDbContextCheck<TaskDbContext>(
                name: "database",
                tags: ["ready"])
            .AddCheck(
                name: "self",
                check: () => HealthCheckResult.Healthy("Application is running"),
                tags: ["live"]);

        return services;
    }
}
