using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TaskFlow.Api.Configuration;

namespace TaskFlow.Api.HealthChecks;

/// <summary>
/// Custom health check response writer that provides detailed JSON responses
/// </summary>
public static class HealthCheckResponseWriter
{
    public static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

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
}
