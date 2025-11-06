namespace TaskFlow.Api.Models;

public class Status
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }

    // Navigation property
    public ICollection<TaskItem> TaskItems { get; set; } = [];
}
