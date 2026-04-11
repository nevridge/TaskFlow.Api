using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using TaskFlow.Api.HealthChecks;
using TaskFlow.Api.Providers;

namespace TaskFlow.Api.Tests.HealthChecks;

/// <summary>
/// Tests to verify health check failures are logged via ILogger
/// </summary>
public class HealthCheckLoggingTests
{
    private static (DefaultHttpContext Context, TestLogger Logger) CreateHttpContextWithLogger()
    {
        var testLogger = new TestLogger();
        var services = new ServiceCollection();
        services.Configure<JsonOptions>(options =>
        {
            JsonSerializerOptionsProvider.ConfigureOptions(options.SerializerOptions);
        });
        services.AddLogging(b => b.AddProvider(new TestLoggerProvider(testLogger)));

        var serviceProvider = services.BuildServiceProvider();
        return (
            new DefaultHttpContext
            {
                RequestServices = serviceProvider,
                Request = { Path = "/health" }
            },
            testLogger
        );
    }

    [Fact]
    public async Task WriteHealthCheckResponse_ShouldLogUnhealthyStatus()
    {
        // Arrange
        var (context, testLogger) = CreateHttpContextWithLogger();
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
        testLogger.Logs.Should().NotBeEmpty("health check failure should be logged");

        var errorLog = testLogger.Logs.FirstOrDefault(e => e.Level == LogLevel.Error);
        errorLog.Should().NotBeNull("unhealthy status should be logged at Error level");
        errorLog!.Message.Should().Contain("Health check FAILED", "log should indicate failure");
        errorLog.Message.Should().Contain("/health", "log should include endpoint");
        errorLog.Message.Should().Contain("Unhealthy", "log should include status");
    }

    [Fact]
    public async Task WriteHealthCheckResponse_ShouldLogDegradedStatus()
    {
        // Arrange
        var (context, testLogger) = CreateHttpContextWithLogger();
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
        testLogger.Logs.Should().NotBeEmpty("degraded health check should be logged");

        var warningLog = testLogger.Logs.FirstOrDefault(e => e.Level == LogLevel.Warning);
        warningLog.Should().NotBeNull("degraded status should be logged at Warning level");
        warningLog!.Message.Should().Contain("Health check DEGRADED", "log should indicate degradation");
        warningLog.Message.Should().Contain("/health/ready", "log should include endpoint");
        warningLog.Message.Should().Contain("Degraded", "log should include status");
    }

    [Fact]
    public async Task WriteHealthCheckResponse_ShouldNotLogHealthyStatus()
    {
        // Arrange
        var (context, testLogger) = CreateHttpContextWithLogger();
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

        // Assert
        testLogger.Logs.Should().BeEmpty("healthy status should not generate logs to reduce noise");
    }

    [Fact]
    public async Task WriteHealthCheckResponse_ShouldLogMultipleFailedChecks()
    {
        // Arrange
        var (context, testLogger) = CreateHttpContextWithLogger();
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
        var errorLog = testLogger.Logs.FirstOrDefault(e => e.Level == LogLevel.Error);
        errorLog.Should().NotBeNull();
        errorLog!.Message.Should().Contain("database", "log should include first failed check");
        errorLog.Message.Should().Contain("external-api", "log should include second failed check");
    }

    [Fact]
    public async Task WriteHealthCheckResponse_ShouldIncludeExceptionInformation()
    {
        // Arrange
        var (context, testLogger) = CreateHttpContextWithLogger();
        context.Request.Path = "/health";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        const string exceptionMessage = "Unable to connect to database server";
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
        var errorLog = testLogger.Logs.FirstOrDefault(e => e.Level == LogLevel.Error);
        errorLog.Should().NotBeNull();
        errorLog!.Message.Should().Contain(exceptionMessage, "log should include exception message from health check");
    }

    /// <summary>
    /// Routes all log entries from every category to a shared <see cref="TestLogger"/>
    /// </summary>
    private sealed class TestLoggerProvider(TestLogger logger) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => logger;
        public void Dispose() { }
    }

    /// <summary>
    /// A simple in-memory ILogger implementation for asserting on captured log entries in tests
    /// </summary>
    private sealed class TestLogger : ILogger
    {
        public record LogEntry(LogLevel Level, string Message);

        public List<LogEntry> Logs { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Logs.Add(new LogEntry(logLevel, formatter(state, exception)));
    }
}
