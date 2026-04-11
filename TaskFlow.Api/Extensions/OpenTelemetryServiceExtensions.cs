using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        var endpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:5341/ingest/otlp";
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "TaskFlow.Api";
        var header = configuration["OpenTelemetry:Header"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = new Uri(endpoint);
                    otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
                    if (!string.IsNullOrEmpty(header))
                        otlp.Headers = header;
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = new Uri(endpoint);
                    otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
                    if (!string.IsNullOrEmpty(header))
                        otlp.Headers = header;
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
        var endpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:5341/ingest/otlp";
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "TaskFlow.Api";
        var header = configuration["OpenTelemetry:Header"];

        logging.ClearProviders();
        logging.AddFilter("Microsoft.AspNetCore.HttpLogging", LogLevel.Information);

        logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;

            options.AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(endpoint);
                otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
                if (!string.IsNullOrEmpty(header))
                    otlp.Headers = header;
            });
        });

        if (environment.IsDevelopment())
            logging.AddConsole();

        return logging;
    }
}
