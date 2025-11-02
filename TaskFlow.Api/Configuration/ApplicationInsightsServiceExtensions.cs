using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace TaskFlow.Api.Configuration;

/// <summary>
/// Extension methods for configuring Application Insights services
/// </summary>
public static class ApplicationInsightsServiceExtensions
{
    /// <summary>
    /// Adds Application Insights telemetry services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationInsights(this IServiceCollection services)
    {
        // Configure Application Insights with adaptive sampling enabled by default
        var options = new ApplicationInsightsServiceOptions
        {
            // Enable adaptive sampling to control telemetry volume and costs
            EnableAdaptiveSampling = true,
            
            // Enable SQL dependency tracking
            EnableDependencyTrackingTelemetryModule = true,
            
            // Enable performance counter collection (if available)
            EnablePerformanceCounterCollectionModule = true,
            
            // Enable request tracking for all HTTP requests
            EnableRequestTrackingTelemetryModule = true,
            
            // Enable event counter collection
            EnableEventCounterCollectionModule = true,
            
            // Connection string is read from configuration
            // Priority: ConnectionString > InstrumentationKey
            ConnectionString = null // Will be read from appsettings.json
        };

        services.AddApplicationInsightsTelemetry(options);

        return services;
    }
}
