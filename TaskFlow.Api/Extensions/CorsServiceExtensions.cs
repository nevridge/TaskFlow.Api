namespace TaskFlow.Api.Extensions;

public static class CorsServiceExtensions
{
    private const string PolicyName = "FrontendPolicy";

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(opts => opts.AddPolicy(PolicyName, policy =>
            policy.WithOrigins(origins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()));

        return services;
    }

    public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app) =>
        app.UseCors(PolicyName);
}
