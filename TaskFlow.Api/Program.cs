using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskFlow.Api.Configuration;
using TaskFlow.Api.Data;
using TaskFlow.Api.HealthChecks;
using TaskFlow.Api.Middleware;

// Configure Serilog bootstrap logger
LoggingServiceExtensions.ConfigureBootstrapLogger();

try
{
    Log.Information("Starting TaskFlow API (bootstrap logger)");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog as the host logger
    builder.Host.AddSerilog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddSwagger();
    builder.Services.AddPersistence(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddValidation();
    builder.Services.AddApplicationHealthChecks();
    builder.Services.ConfigureJsonSerialization();

    var app = builder.Build();

    // Apply EF migrations
    using (var scope = app.Services.CreateScope())
    {
        var env = app.Environment;
        var shouldMigrate = env.IsDevelopment()
                            || builder.Configuration.GetValue<bool>("Database:MigrateOnStartup");

        if (shouldMigrate)
        {
            Log.Information("Applying EF Core migrations on startup (Environment: {Env})", env.EnvironmentName);
            var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();

            // Ensure the directory exists for SQLite
            var dbPath = db.Database.GetConnectionString();
            if (!string.IsNullOrEmpty(dbPath) && dbPath.StartsWith("Data Source="))
            {
                var filePath = dbPath.Replace("Data Source=", "").Split(';')[0];
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Log.Information("Created database directory: {Directory}", directory);
                }
            }

            db.Database.Migrate();
        }
        else
        {
            Log.Information("Skipping automatic migrations on startup (Environment: {Env})", env.EnvironmentName);
        }
    }

    // Enable Swagger UI in Development
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1");
            c.RoutePrefix = string.Empty;
        });
    }

    app.UseMiddleware<ValidationMiddleware>();

    // Skip HTTPS redirection in containers
    if (!app.Environment.IsEnvironment("Container") &&
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")))
    {
        app.UseHttpsRedirection();
    }

    app.MapControllers();

    // Map health check endpoints with custom JSON response writer and failure logging
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse,
        ResultStatusCodes =
        {
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy] = StatusCodes.Status200OK,
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = StatusCodes.Status200OK,
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = (check) => check.Tags.Contains("ready"),
        ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse,
        ResultStatusCodes =
        {
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy] = StatusCodes.Status200OK,
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = StatusCodes.Status200OK,
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = (check) => check.Tags.Contains("live"),
        ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse,
        ResultStatusCodes =
        {
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy] = StatusCodes.Status200OK,
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = StatusCodes.Status200OK,
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });

    Log.Information("Starting web host on port {Port}", Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080");
    app.Run();
}
catch (Exception ex)
{
    // Ensure serious startup errors are captured
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    // Ensure all logs are flushed and sinks are disposed
    Log.CloseAndFlush();
}
