using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskFlow.Api.HealthChecks;

namespace TaskFlow.Api.Tests.HealthChecks;

public class HealthCheckResponseWriterTests
{
    [Fact]
    public async Task WriteHealthCheckResponse_ShouldWriteHealthyStatus()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "Database is healthy",
                    TimeSpan.FromMilliseconds(50),
                    null,
                    null)
            },
            TimeSpan.FromMilliseconds(100));

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(context, healthReport);

        // Assert
        responseBody.Position = 0;
        var reader = new StreamReader(responseBody);
        var response = await reader.ReadToEndAsync();
        
        response.Should().NotBeNullOrEmpty();
        response.Should().Contain("Healthy");
        response.Should().Contain("database");
        context.Response.ContentType.Should().Be("application/json; charset=utf-8");
    }

    [Fact]
    public async Task WriteHealthCheckResponse_ShouldWriteUnhealthyStatus()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    "Database connection failed",
                    TimeSpan.FromMilliseconds(50),
                    new InvalidOperationException("Connection error"),
                    null)
            },
            TimeSpan.FromMilliseconds(100));

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(context, healthReport);

        // Assert
        responseBody.Position = 0;
        var reader = new StreamReader(responseBody);
        var response = await reader.ReadToEndAsync();
        
        response.Should().NotBeNullOrEmpty();
        response.Should().Contain("Unhealthy");
        response.Should().Contain("database");
        response.Should().Contain("Connection error");
    }

    [Fact]
    public async Task WriteHealthCheckResponse_ShouldIncludeMultipleEntries()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "Database is healthy",
                    TimeSpan.FromMilliseconds(30),
                    null,
                    null),
                ["api"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "API is running",
                    TimeSpan.FromMilliseconds(10),
                    null,
                    null)
            },
            TimeSpan.FromMilliseconds(50));

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(context, healthReport);

        // Assert
        responseBody.Position = 0;
        var reader = new StreamReader(responseBody);
        var response = await reader.ReadToEndAsync();
        
        response.Should().Contain("database");
        response.Should().Contain("api");
    }

    [Fact]
    public async Task WriteHealthCheckResponse_ShouldHandleDegradedStatus()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["service"] = new HealthReportEntry(
                    HealthStatus.Degraded,
                    "Service is degraded",
                    TimeSpan.FromMilliseconds(100),
                    null,
                    null)
            },
            TimeSpan.FromMilliseconds(150));

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(context, healthReport);

        // Assert
        responseBody.Position = 0;
        var reader = new StreamReader(responseBody);
        var response = await reader.ReadToEndAsync();
        
        response.Should().Contain("Degraded");
    }

    [Fact]
    public async Task WriteHealthCheckResponse_ShouldIncludeCustomData()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var customData = new Dictionary<string, object>
        {
            ["ConnectionString"] = "Server=localhost",
            ["Version"] = "1.0.0"
        };

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "Database is healthy",
                    TimeSpan.FromMilliseconds(50),
                    null,
                    customData)
            },
            TimeSpan.FromMilliseconds(100));

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(context, healthReport);

        // Assert
        responseBody.Position = 0;
        var reader = new StreamReader(responseBody);
        var response = await reader.ReadToEndAsync();
        
        response.Should().Contain("ConnectionString");
        response.Should().Contain("Version");
    }
}
