using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using TaskFlow.Api.Configuration;
using TaskFlow.Api.HealthChecks;

namespace TaskFlow.Api.Tests.HealthChecks;

/// <summary>
/// Collection definition to ensure tests run sequentially (InMemorySink is global)
/// </summary>
[CollectionDefinition("HealthCheckLogging", DisableParallelization = true)]
public class HealthCheckLoggingCollection
{
}

/// <summary>
/// Tests to verify health check failures are logged by Serilog
/// </summary>
[Collection("HealthCheckLogging")]
public class HealthCheckLoggingTests : IDisposable
{
    private readonly ILogger _originalLogger;

    public HealthCheckLoggingTests()
    {
        // Save the original logger
        _originalLogger = Log.Logger;

        // Clear any existing in-memory logs from previous tests
        InMemorySink.Instance.Dispose();

        // Configure Serilog to write to an in-memory sink for testing
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.InMemory()
            .CreateLogger();
    }

    public void Dispose()
    {
        // Clear in-memory logs
        InMemorySink.Instance.Dispose();

        // Restore the original logger
        Log.Logger = _originalLogger;
    }

    private static DefaultHttpContext CreateHttpContextWithServices()
    {
        var services = new ServiceCollection();
        services.Configure<JsonOptions>(options =>
        {
            JsonSerializerOptionsProvider.ConfigureOptions(options.SerializerOptions);
        });
        var serviceProvider = services.BuildServiceProvider();
        return new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            Request = { Path = "/health" }
        };
    }

    [Fact]
    public async Task WriteHealthCheckResponse_ShouldLogUnhealthyStatus()
    {
        // Arrange
        var context = CreateHttpContextWithServices();
        context.Request.Path = "/health";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    "Database connection failed",
                    TimeSpan.FromMilliseconds(5000),
                    new InvalidOperationException("Connection error"),
                    null)
            },
            TimeSpan.FromMilliseconds(5100));

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(context, healthReport);

        // Assert
        var logEvents = InMemorySink.Instance.LogEvents.ToList();
        logEvents.Should().NotBeEmpty("health check failure should be logged");

        var errorLog = logEvents.FirstOrDefault(e => e.Level == LogEventLevel.Error);
        errorLog.Should().NotBeNull("unhealthy status should be logged at Error level");

        var message = errorLog?.RenderMessage();
        message.Should().Contain("Health check FAILED", "log should indicate failure");
        message.Should().Contain("/health", "log should include endpoint");
        message.Should().Contain("Unhealthy", "log should include status");
    }

    [Fact]
    public async Task WriteHealthCheckResponse_ShouldLogDegradedStatus()
    {
        // Arrange
        var context = CreateHttpContextWithServices();
        context.Request.Path = "/health/ready";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Degraded,
                    "High response time",
                    TimeSpan.FromMilliseconds(3000),
                    null,
                    null)
            },
            TimeSpan.FromMilliseconds(3100));

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(context, healthReport);

        // Assert
        var logEvents = InMemorySink.Instance.LogEvents.ToList();
        logEvents.Should().NotBeEmpty("degraded health check should be logged");

        var warningLog = logEvents.FirstOrDefault(e => e.Level == LogEventLevel.Warning);
        warningLog.Should().NotBeNull("degraded status should be logged at Warning level");

        var message = warningLog?.RenderMessage();
        message.Should().Contain("Health check DEGRADED", "log should indicate degradation");
        message.Should().Contain("/health/ready", "log should include endpoint");
        message.Should().Contain("Degraded", "log should include status");
    }

    [Fact]
    public async Task WriteHealthCheckResponse_ShouldNotLogHealthyStatus()
    {
        // Arrange
        var context = CreateHttpContextWithServices();
        context.Request.Path = "/health/live";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["self"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "Application is running",
                    TimeSpan.FromMilliseconds(10),
                    null,
                    null)
            },
            TimeSpan.FromMilliseconds(15));

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(context, healthReport);

        // Assert - filter for health check related logs only
        var healthCheckLogs = InMemorySink.Instance.LogEvents
            .Where(e => e.MessageTemplate.Text.Contains("Health check"))
            .ToList();
        healthCheckLogs.Should().BeEmpty("healthy status should not generate logs to reduce noise");
    }

    [Fact]
    public async Task WriteHealthCheckResponse_ShouldLogMultipleFailedChecks()
    {
        // Arrange
        var context = CreateHttpContextWithServices();
        context.Request.Path = "/health";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    "Database connection failed",
                    TimeSpan.FromMilliseconds(5000),
                    new InvalidOperationException("Connection error"),
                    null),
                ["external-api"] = new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    "API timeout",
                    TimeSpan.FromMilliseconds(3000),
                    new TimeoutException("Request timeout"),
                    null)
            },
            TimeSpan.FromMilliseconds(8000));

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(context, healthReport);

        // Assert
        var logEvents = InMemorySink.Instance.LogEvents.ToList();
        var errorLog = logEvents.FirstOrDefault(e => e.Level == LogEventLevel.Error);
        errorLog.Should().NotBeNull();

        var message = errorLog?.RenderMessage();
        message.Should().Contain("database", "log should include first failed check");
        message.Should().Contain("external-api", "log should include second failed check");
    }

    [Fact]
    public async Task WriteHealthCheckResponse_ShouldIncludeExceptionInformation()
    {
        // Arrange
        var context = CreateHttpContextWithServices();
        context.Request.Path = "/health";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var exceptionMessage = "Unable to connect to database server";
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    "Database connection failed",
                    TimeSpan.FromMilliseconds(5000),
                    new InvalidOperationException(exceptionMessage),
                    null)
            },
            TimeSpan.FromMilliseconds(5100));

        // Act
        await HealthCheckResponseWriter.WriteHealthCheckResponse(context, healthReport);

        // Assert
        var logEvents = InMemorySink.Instance.LogEvents.ToList();
        var errorLog = logEvents.FirstOrDefault(e => e.Level == LogEventLevel.Error);

        var message = errorLog?.RenderMessage();
        message.Should().Contain(exceptionMessage, "log should include exception message from health check");
    }
}
