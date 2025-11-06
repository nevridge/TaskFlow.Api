namespace TaskFlow.Api.DTOs;

public class TaskItemResponseDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public bool IsComplete { get; set; }
    public string? StatusName { get; set; } // Flattened - just the status name
}