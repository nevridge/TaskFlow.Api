using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskFlow.Api.Data;
using TaskFlow.Api.HealthChecks;

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

    /// <summary>
    /// Creates health check options with logging and standard status codes
    /// </summary>
    /// <returns>Configured health check options</returns>
    public static HealthCheckOptions CreateHealthCheckOptions()
    {
        return new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        };
    }

    /// <summary>
    /// Creates health check options for readiness probe (database checks)
    /// </summary>
    /// <returns>Configured health check options for readiness</returns>
    public static HealthCheckOptions CreateReadinessHealthCheckOptions()
    {
        var options = CreateHealthCheckOptions();
        options.Predicate = check => check.Tags.Contains("ready");
        return options;
    }

    /// <summary>
    /// Creates health check options for liveness probe (self checks)
    /// </summary>
    /// <returns>Configured health check options for liveness</returns>
    public static HealthCheckOptions CreateLivenessHealthCheckOptions()
    {
        var options = CreateHealthCheckOptions();
        options.Predicate = check => check.Tags.Contains("live");
        return options;
    }
}
