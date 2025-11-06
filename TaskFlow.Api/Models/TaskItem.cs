namespace TaskFlow.Api.Models;

public class TaskItem
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public bool IsComplete { get; set; }

    // Add foreign key and navigation property
    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;
}
