using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using TaskFlow.Api.Data;
using TaskFlow.Api.Middleware;
using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;
using TaskFlow.Api.Services;
using TaskFlow.Api.Validators;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting TaskFlow API (bootstrap logger)");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog as the host logger (will use the static Log.Logger)
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer(); // useful for minimal APIs and Swagger discovery
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskFlow API", Version = "v1" });
    });

    // Read connection string from configuration (appsettings.json, environment, or secrets)
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? "Data Source=tasks.db";

    // Register SQLite DB context BEFORE calling Build()
    builder.Services.AddDbContext<TaskDbContext>(options =>
        options.UseSqlite(connectionString));

    builder.Services.AddScoped<ITaskRepository, TaskRepository>();
    builder.Services.AddScoped<TaskService>();
    // Register validators
    builder.Services.AddValidatorsFromAssemblyContaining<TaskItemValidator>();

    var app = builder.Build();

    // Apply EF migrations conditionally to avoid surprises in production:
    // - Always auto-migrate in Development (convenience)
    // - In other environments, only apply if config "Database:MigrateOnStartup" is true
    //   (set via appsettings, environment var DATABASE__MigrateOnStartup, or secrets)
    using (var scope = app.Services.CreateScope())
    {
        var env = app.Environment;
        var shouldMigrate = env.IsDevelopment()
                            || builder.Configuration.GetValue<bool>("Database:MigrateOnStartup");

        if (shouldMigrate)
        {
            Log.Information("Applying EF Core migrations on startup (Environment: {Env})", env.EnvironmentName);
            var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
            db.Database.Migrate(); // requires migrations to be present
        }
        else
        {
            Log.Information("Skipping automatic migrations on startup (Environment: {Env})", env.EnvironmentName);
        }
    }

    // Enable Swagger UI in Development (recommended)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1");
            c.RoutePrefix = string.Empty; // serve at root
        });
    }

    // Add the middleware
    app.UseMiddleware<ValidationMiddleware>();

    // Only use HTTPS redirection when not running in a container
    if (!app.Environment.IsEnvironment("Container") && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")))
    {
        app.UseHttpsRedirection();
    }
    
    app.MapControllers();

    Log.Information("Starting web host");
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
