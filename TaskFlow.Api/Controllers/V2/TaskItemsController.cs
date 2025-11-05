using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TaskItemsController(ITaskService taskService, IValidator<TaskItem> validator) : ControllerBase
{
    private const string ApiVersionString = "2.0";
    
    private readonly ITaskService _taskService = taskService;
    private readonly IValidator<TaskItem> _validator = validator;

    // GET: api/v2/TaskItems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItemResponseDto>>> GetAll()
    {
        var items = await _taskService.GetAllTasksAsync();
        var response = items.Select(MapToResponseDto);
        return Ok(response);
    }

    // GET: api/v2/TaskItems/5
    [HttpGet("{id}", Name = "GetTaskV2")]
    public async Task<ActionResult<TaskItemResponseDto>> Get(int id)
    {
        var item = await _taskService.GetTaskAsync(id);

        if (item is null) return NotFound();
        return Ok(MapToResponseDto(item));
    }

    // POST: api/v2/TaskItems
    [HttpPost]
    public async Task<ActionResult<TaskItemResponseDto>> Create([FromBody] CreateTaskItemDto createDto)
    {
        var item = new TaskItem
        {
            Title = createDto.Title,
            Description = createDto.Description,
            IsComplete = createDto.IsComplete
        };

        var validationResult = await _validator.ValidateAsync(item);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var createdItem = await _taskService.CreateTaskAsync(item);
        var response = MapToResponseDto(createdItem);

        return CreatedAtRoute("GetTaskV2", new { version = ApiVersionString, id = createdItem.Id }, response);
    }

    // PUT: api/v2/TaskItems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskItemDto updateDto)
    {
        var existing = await _taskService.GetTaskAsync(id);
        if (existing is null) return NotFound();

        // Apply incoming changes
        existing.Title = updateDto.Title;
        existing.Description = updateDto.Description;
        existing.IsComplete = updateDto.IsComplete;

        var validationResult = await _validator.ValidateAsync(existing);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        await _taskService.UpdateTaskAsync(existing);

        return NoContent();
    }

    // DELETE: api/v2/TaskItems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _taskService.GetTaskAsync(id);
        if (existing is null) return NotFound();

        await _taskService.DeleteTaskAsync(id);

        return NoContent();
    }

    /// <summary>
    /// Maps a TaskItem to TaskItemResponseDto with metadata
    /// </summary>
    private static TaskItemResponseDto MapToResponseDto(TaskItem item)
    {
        return new TaskItemResponseDto
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            IsComplete = item.IsComplete,
            Metadata = new ResponseMetadata
            {
                ApiVersion = ApiVersionString,
                Timestamp = DateTime.UtcNow
            }
        };
    }
}
