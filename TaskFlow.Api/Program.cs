using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using TaskFlow.Api.Data;
using TaskFlow.Api.HealthChecks;
using TaskFlow.Api.Middleware;
using TaskFlow.Api.Repositories;
using TaskFlow.Api.Services;
using TaskFlow.Api.Validators;
using TaskFlow.Api.Configuration;

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

try
{
    Log.Information("Starting TaskFlow API (bootstrap logger)");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog as the host logger (will use the static Log.Logger)
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskFlow API", Version = "v1" });
    });

    // Read connection string from configuration
    // Default to /app/data/tasks.db for consistent container paths
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? "Data Source=/app/data/tasks.db";

    Log.Information("Using connection string: {ConnectionString}", connectionString);

    // Register SQLite DB context
    builder.Services.AddDbContext<TaskDbContext>(options =>
        options.UseSqlite(connectionString));

    builder.Services.AddScoped<ITaskRepository, TaskRepository>();
    builder.Services.AddScoped<ITaskService, TaskService>();
    // Register validators
    builder.Services.AddValidatorsFromAssemblyContaining<TaskItemValidator>();

    // Add health checks with database connectivity validation
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<TaskDbContext>(
            name: "database",
            tags: ["ready"])
        .AddCheck(
            name: "self",
            check: () => HealthCheckResult.Healthy("Application is running"),
            tags: ["live"]);

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

    // Map health check endpoints with custom JSON response writer
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse
    });
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = (check) => check.Tags.Contains("ready"),
        ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = (check) => check.Tags.Contains("live"),
        ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse
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
