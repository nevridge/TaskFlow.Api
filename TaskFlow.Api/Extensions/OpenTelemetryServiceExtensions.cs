using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TaskFlow.Api.Extensions;

/// <summary>
/// Extension methods for configuring OpenTelemetry observability (tracing, metrics, and logging)
/// </summary>
public static class OpenTelemetryServiceExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing and metrics, exporting via OTLP to a Seq instance
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration used to read endpoint and service name</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddOpenTelemetryObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseEndpoint = (configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:5341/ingest/otlp").TrimEnd('/');
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "TaskFlow.Api";
        var header = configuration["OpenTelemetry:Header"];
        var protocol = ParseProtocol(configuration["OpenTelemetry:Protocol"]);

        services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestMethod
                | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath
                | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode
                | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.Duration;
        });

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = new Uri(baseEndpoint + "/v1/traces");
                    otlp.Protocol = protocol;
                    if (!string.IsNullOrEmpty(header))
                    {
                        otlp.Headers = header;
                    }
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = new Uri(baseEndpoint + "/v1/metrics");
                    otlp.Protocol = protocol;
                    if (!string.IsNullOrEmpty(header))
                    {
                        otlp.Headers = header;
                    }
                }));

        return services;
    }

    /// <summary>
    /// Clears default logging providers and routes logs to Seq via OTLP.
    /// A console provider is added when running in the Development environment.
    /// </summary>
    /// <param name="logging">The logging builder</param>
    /// <param name="configuration">Application configuration used to read endpoint and API key header</param>
    /// <param name="environment">Host environment used to detect Development mode</param>
    /// <returns>The logging builder for chaining</returns>
    public static ILoggingBuilder AddApplicationLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var baseEndpoint = (configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:5341/ingest/otlp").TrimEnd('/');
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "TaskFlow.Api";
        var header = configuration["OpenTelemetry:Header"];
        var protocol = ParseProtocol(configuration["OpenTelemetry:Protocol"]);

        logging.ClearProviders();
        logging.AddFilter("Microsoft.AspNetCore.HttpLogging", LogLevel.Information);

        logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;

            options.AddOtlpExporter((otlp, processor) =>
            {
                otlp.Endpoint = new Uri(baseEndpoint + "/v1/logs");
                otlp.Protocol = protocol;
                if (!string.IsNullOrEmpty(header))
                {
                    otlp.Headers = header;
                }
                // Simple processor in Development exports each record immediately, so logs
                // are visible in Seq without delay and are not lost when VS stops the
                // container with SIGKILL before the batch can flush.
                processor.ExportProcessorType = environment.IsDevelopment()
                    ? ExportProcessorType.Simple
                    : ExportProcessorType.Batch;
            });
        });

        if (environment.IsDevelopment())
        {
            logging.AddConsole();
        }

        return logging;
    }

    /// <summary>
    /// Parses the OTLP export protocol from a configuration string.
    /// The only supported value is "http/protobuf" (case-insensitive).
    /// Defaults to <see cref="OtlpExportProtocol.HttpProtobuf"/> when the value is null or empty.
    /// </summary>
    /// <param name="protocolValue">The protocol string from configuration</param>
    /// <returns>The corresponding <see cref="OtlpExportProtocol"/> value</returns>
    /// <exception cref="InvalidOperationException">Thrown when the protocol value is not supported</exception>
    private static OtlpExportProtocol ParseProtocol(string? protocolValue) =>
        protocolValue?.ToLowerInvariant() switch
        {
            null or "" or "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
            _ => throw new InvalidOperationException(
                $"Unsupported OpenTelemetry protocol '{protocolValue}'. The only supported value is 'http/protobuf'.")
        };
}
