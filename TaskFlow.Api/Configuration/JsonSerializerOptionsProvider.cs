using System.Text.Json;
using System.Text.Json.Serialization;

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
        options.ReferenceHandler = ReferenceHandler.IgnoreCycles; // Handle circular references
    }

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ConfigureOptions(options);
        options.MakeReadOnly(); // Prevents accidental modifications
        return options;
    }
}
