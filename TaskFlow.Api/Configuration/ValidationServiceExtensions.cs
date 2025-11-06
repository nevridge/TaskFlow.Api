using FluentValidation;
using TaskFlow.Api.Validators;

namespace TaskFlow.Api.Configuration;

/// <summary>
/// Extension methods for configuring validation services
/// </summary>
public static class ValidationServiceExtensions
{
    /// <summary>
    /// Adds FluentValidation validators to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        // Register all validators from the assembly containing TaskItemValidator
        // This automatically includes TaskItemValidator, StatusValidator, and any future validators
        services.AddValidatorsFromAssemblyContaining<TaskItemValidator>();

        return services;
    }
}
