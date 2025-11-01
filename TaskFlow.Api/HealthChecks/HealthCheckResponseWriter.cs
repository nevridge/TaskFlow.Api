using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskFlow.Api.HealthChecks;

/// <summary>
/// Custom health check response writer that provides detailed JSON responses
/// </summary>
public static class HealthCheckResponseWriter
{
    public static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

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
        catch (Exception ex)
        {
            // Fallback to simple error response if serialization fails
            var errorResponse = $"{{\"status\":\"Unhealthy\",\"error\":\"Failed to serialize health check response: {ex.Message}\"}}";
            await context.Response.WriteAsync(errorResponse);
        }
    }
}
