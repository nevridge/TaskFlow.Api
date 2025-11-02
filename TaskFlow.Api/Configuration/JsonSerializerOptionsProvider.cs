using System.Text.Json;

namespace TaskFlow.Api.Configuration;

public static class JsonSerializerOptionsProvider
{
    private static readonly JsonSerializerOptions _defaultOptions = CreateDefaultOptions();

    public static JsonSerializerOptions Default => _defaultOptions;

    /// <summary>
    /// Configures JsonSerializerOptions with application-wide settings
    /// </summary>
    public static void ConfigureOptions(JsonSerializerOptions options)
    {
        options.WriteIndented = true;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            // makeReadOnly: true prevents modification after initialization
        };
        ConfigureOptions(options);
        options.MakeReadOnly();
        return options;
    }
}