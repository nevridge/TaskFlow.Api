namespace TaskFlow.Api.DTOs;

public class StatusResponseDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
