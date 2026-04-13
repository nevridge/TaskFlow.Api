using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Repositories;

namespace TaskFlow.Api.Extensions;

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

        // Register SQLite DB context
        services.AddDbContext<TaskDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register repositories
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IStatusRepository, StatusRepository>();

        return services;
    }

    /// <summary>
    /// Ensures the directory for a SQLite Data Source connection string exists,
    /// creating it if necessary. No-ops for in-memory or non-file connection strings.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string</param>
    /// <param name="logger">Optional logger for directory creation events</param>
    internal static void EnsureSqliteDirectoryExists(string? connectionString, ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(connectionString) || !connectionString.StartsWith("Data Source="))
            return;

        var filePath = connectionString.Replace("Data Source=", "").Split(';')[0];
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            logger?.LogInformation("Created database directory: {Directory}", directory);
        }
    }
}
