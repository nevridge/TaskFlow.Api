using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Api.Providers;

namespace TaskFlow.Api.HealthChecks;

/// <summary>
/// Custom health check response writer that provides detailed JSON responses and logs failures
/// </summary>
public static class HealthCheckResponseWriter
{
    public static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var logger = context.RequestServices.GetService<ILoggerFactory>()
            ?.CreateLogger(nameof(HealthCheckResponseWriter));

        // Log health check failures with appropriate severity
        if (report.Status == HealthStatus.Unhealthy)
        {
            LogHealthCheckFailure(logger, context.Request.Path, report);
        }
        else if (report.Status == HealthStatus.Degraded)
        {
            LogHealthCheckDegraded(logger, context.Request.Path, report);
        }

        // Retrieve options from DI container
        var options = context.RequestServices
            .GetService<IOptions<JsonOptions>>()?.Value?.SerializerOptions
            ?? JsonSerializerOptionsProvider.Default;

        try
        {
            var result = new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                results = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description ?? string.Empty,
                    duration = entry.Value.Duration.TotalMilliseconds,
                    exception = entry.Value.Exception?.Message,
                    data = entry.Value.Data.Count > 0 ? entry.Value.Data : null
                })
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(result, options));
        }
        catch (JsonException ex)
        {
            // Fallback to simple error response if serialization fails
            var errorResponse = $"{{\"status\":\"Unhealthy\",\"error\":\"Failed to serialize health check response: {ex.Message}\"}}";
            await context.Response.WriteAsync(errorResponse);
        }
        catch (InvalidOperationException ex)
        {
            // Fallback to simple error response if serialization fails
            var errorResponse = $"{{\"status\":\"Unhealthy\",\"error\":\"Failed to serialize health check response: {ex.Message}\"}}";
            await context.Response.WriteAsync(errorResponse);
        }
    }

    private static void LogHealthCheckFailure(ILogger? logger, string endpoint, HealthReport report)
    {
        if (logger is null) return;

        var failedChecks = report.Entries
            .Where(e => e.Value.Status == HealthStatus.Unhealthy)
            .Select(e => $"{e.Key}: {e.Value.Description ?? "no description"}, exception: {e.Value.Exception?.Message ?? "none"}");

        logger.LogError(
            "Health check FAILED at {Endpoint} - Status: {Status}, Duration: {Duration}ms, Failed checks: {FailedChecks}",
            endpoint,
            report.Status,
            report.TotalDuration.TotalMilliseconds,
            string.Join("; ", failedChecks));
    }

    private static void LogHealthCheckDegraded(ILogger? logger, string endpoint, HealthReport report)
    {
        if (logger is null) return;

        var degradedChecks = report.Entries
            .Where(e => e.Value.Status == HealthStatus.Degraded)
            .Select(e => $"{e.Key}: {e.Value.Description ?? "no description"}");

        logger.LogWarning(
            "Health check DEGRADED at {Endpoint} - Status: {Status}, Duration: {Duration}ms, Degraded checks: {DegradedChecks}",
            endpoint,
            report.Status,
            report.TotalDuration.TotalMilliseconds,
            string.Join("; ", degradedChecks));
    }
}
