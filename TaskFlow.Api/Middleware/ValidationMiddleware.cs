using FluentValidation;
using System.Text.Json;

namespace TaskFlow.Api.Middleware;

public class ValidationMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
{
    private readonly RequestDelegate _next = next;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate JSON requests with a body for safety
        if (context.Request.ContentLength == null
            || context.Request.ContentLength == 0
            || context.Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) != true
            || HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method)
            || HttpMethods.IsDelete(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Enable buffering so the controller can still read the body
        context.Request.EnableBuffering();

        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
        {
            await _next(context);
            return;
        }

        // Resolve all validators registered in DI (IValidator and IValidator<T>)
        // Note: GetServices<IValidator>() returns non-generic IValidator instances
        var validators = _serviceProvider.GetServices<IValidator>().ToList();

        foreach (var validator in validators)
        {
            // Get the specific T type the validator is for (IValidator<T>)
            var validatorInterface = validator.GetType()
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));

            if (validatorInterface == null) continue;

            var modelType = validatorInterface.GetGenericArguments()[0];
            object? model;
            try
            {
                model = JsonSerializer.Deserialize(body, modelType, _jsonOptions);
            }
            catch (JsonException)
            {
                // If deserialization fails for this model type, skip it
                continue;
            }
            catch (NotSupportedException)
            {
                // If deserialization fails for this model type, skip it
                continue;
            }

            if (model == null) continue;

            // Resolve strongly-typed IValidator<T> to run validation
            if (_serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(modelType)) is not IValidator typedValidator) continue;

            var contextType = typeof(ValidationContext<>).MakeGenericType(modelType);
            var contextValidation = Activator.CreateInstance(contextType, model);
            var result = await (Task<FluentValidation.Results.ValidationResult>)
                typedValidator.GetType()
                .GetMethod("ValidateAsync", new[] { contextType, typeof(CancellationToken) })!
                .Invoke(typedValidator, new[] { contextValidation, CancellationToken.None });

            if (!result.IsValid)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                var errors = result.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }).ToList();
                await context.Response.WriteAsJsonAsync(errors, _jsonOptions);
                return;
            }
        }

        await _next(context);
    }
}
