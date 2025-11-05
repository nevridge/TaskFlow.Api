namespace TaskFlow.Api.DTOs;

/// <summary>
/// Enhanced response DTO with metadata for API v2.0
/// </summary>
public class TaskItemResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsComplete { get; set; }
    
    /// <summary>
    /// Response metadata (added in v2.0)
    /// </summary>
    public ResponseMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Response metadata included in API v2.0 responses
/// </summary>
public class ResponseMetadata
{
    public string ApiVersion { get; set; } = "2.0";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
