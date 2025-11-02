using System.Text.Json;

namespace TaskFlow.Api.Configuration;

public static class JsonSerializerOptionsProvider
{
    private static readonly JsonSerializerOptions _defaultOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static JsonSerializerOptions Default => _defaultOptions;

    /// <summary>
    /// Configures JsonSerializerOptions with application-wide settings
    /// </summary>
    public static void ConfigureOptions(JsonSerializerOptions options)
    {
        options.WriteIndented = true;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }
}