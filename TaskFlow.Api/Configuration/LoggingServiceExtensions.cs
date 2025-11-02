using Microsoft.Extensions.Hosting;
using Serilog;

namespace TaskFlow.Api.Configuration;

/// <summary>
/// Extension methods for configuring logging services
/// </summary>
public static class LoggingServiceExtensions
{
    /// <summary>
    /// Configures Serilog as the logging provider
    /// </summary>
    /// <param name="builder">The host builder</param>
    /// <returns>The host builder for chaining</returns>
    public static ConfigureHostBuilder AddSerilog(this ConfigureHostBuilder builder)
    {
        builder.UseSerilog();
        return builder;
    }

    /// <summary>
    /// Creates and configures the Serilog bootstrap logger
    /// </summary>
    /// <remarks>
    /// This should be called early in application startup before creating the host builder
    /// </remarks>
    public static void ConfigureBootstrapLogger()
    {
        // Configure Serilog with safe paths for containers
        // LOG_PATH can be overridden via environment variable for flexibility
        const string DefaultLogPath = "/app/logs/log.txt";
        var logPath = Environment.GetEnvironmentVariable("LOG_PATH") ?? DefaultLogPath;
        
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();
    }
}
