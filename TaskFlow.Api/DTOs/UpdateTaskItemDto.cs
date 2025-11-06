namespace TaskFlow.Api.DTOs;

public class UpdateTaskItemDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusId { get; set; }
    public bool IsComplete { get; set; }
}
