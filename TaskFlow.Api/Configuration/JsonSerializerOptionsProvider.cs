using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskFlow.Api.Configuration;

public static class JsonSerializerOptionsProvider
{
    private static readonly JsonSerializerOptions _defaultOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static JsonSerializerOptions Default => _defaultOptions;
}