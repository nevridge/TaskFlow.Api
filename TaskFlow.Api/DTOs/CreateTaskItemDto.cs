using TaskFlow.Api.Models;

namespace TaskFlow.Api.DTOs;

public class CreateTaskItemDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusId { get; set; } = 1; // Default to 'To Do' status
    public bool IsComplete { get; set; }
    public Priority Priority { get; set; } = Priority.Low;
    public DateTime? DueDate { get; set; }
}
