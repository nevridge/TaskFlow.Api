using TaskFlow.Api.Providers;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace TaskFlow.Api.Extensions;

public static class JsonConfigurationExtensions
{
    /// <summary>
    /// Configures JSON serialization for both HTTP APIs and MVC controllers
    /// using shared configuration settings
    /// </summary>
    public static IServiceCollection ConfigureJsonSerialization(this IServiceCollection services)
    {
        // Configure for minimal APIs and health checks
        services.ConfigureHttpJsonOptions(options =>
        {
            JsonSerializerOptionsProvider.ConfigureOptions(options.SerializerOptions);
        });

        // Configure for MVC controllers
        services.Configure<MvcJsonOptions>(options =>
        {
            JsonSerializerOptionsProvider.ConfigureOptions(options.JsonSerializerOptions);
        });

        return services;
    }
}
