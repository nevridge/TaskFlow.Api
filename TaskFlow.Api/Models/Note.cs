namespace TaskFlow.Api.Models;

public class Note
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public int TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
