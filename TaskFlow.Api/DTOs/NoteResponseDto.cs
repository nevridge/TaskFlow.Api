namespace TaskFlow.Api.DTOs;

public class NoteResponseDto
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public int TaskItemId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
