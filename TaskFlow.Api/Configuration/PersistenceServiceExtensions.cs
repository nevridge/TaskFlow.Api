using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskFlow.Api.Data;
using TaskFlow.Api.Repositories;

namespace TaskFlow.Api.Configuration;

/// <summary>
/// Extension methods for configuring persistence services
/// </summary>
public static class PersistenceServiceExtensions
{
    /// <summary>
    /// Adds database context and repository services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Read connection string from configuration
        // Default to /app/data/tasks.db for consistent container paths
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Data Source=/app/data/tasks.db";

        Log.Information("Using connection string: {ConnectionString}", connectionString);

        // Register SQLite DB context
        services.AddDbContext<TaskDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register repositories
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IStatusRepository, StatusRepository>();

        return services;
    }
}
