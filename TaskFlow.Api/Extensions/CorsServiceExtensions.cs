namespace TaskFlow.Api.Extensions;

public static class CorsServiceExtensions
{
    private const string PolicyName = "FrontendPolicy";

    public static string[] GetConfiguredOrigins(IConfiguration configuration) =>
        configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var origins = GetConfiguredOrigins(configuration);
        if (origins.Length == 0)
        {
            return services;
        }

        services.AddCors(opts => opts.AddPolicy(PolicyName, policy =>
            policy.WithOrigins(origins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()));

        return services;
    }

    public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app) =>
        app.UseCors(PolicyName);
}
