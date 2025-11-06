using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TaskItemsController(ITaskService taskService, IValidator<TaskItem> validator) : ControllerBase
{
    private const string ApiVersionString = "1.0";
    private readonly ITaskService _taskService = taskService;
    private readonly IValidator<TaskItem> _validator = validator;

    // GET: api/v1/TaskItems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItemResponseDto>>> GetAll()
    {
        var items = await _taskService.GetAllTasksAsync();
        var dtos = items.Select(i => new TaskItemResponseDto
        {
            Id = i.Id,
            Title = i.Title,
            Description = i.Description,
            IsComplete = i.IsComplete,
            StatusName = i.Status?.Name
        });
        return Ok(dtos);
    }

    // GET: api/v1/TaskItems/5
    [HttpGet("{id}", Name = "GetTaskV1")]
    public async Task<ActionResult<TaskItemResponseDto>> Get(int id)
    {
        var item = await _taskService.GetTaskAsync(id);
        if (item is null) return NotFound();

        var dto = new TaskItemResponseDto
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            IsComplete = item.IsComplete,
            StatusName = item.Status?.Name
        };
        return Ok(dto);
    }

    // POST: api/v1/TaskItems
    [HttpPost]
    public async Task<ActionResult<TaskItem>> Create([FromBody] CreateTaskItemDto createDto)
    {
        var item = new TaskItem
        {
            Title = createDto.Title,
            Description = createDto.Description,
            StatusId = createDto.StatusId,
            IsComplete = createDto.IsComplete
        };

        var validationResult = await _validator.ValidateAsync(item);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var createdItem = await _taskService.CreateTaskAsync(item);

        return CreatedAtRoute("GetTaskV1", new { version = ApiVersionString, id = createdItem.Id }, createdItem);
    }

    // PUT: api/v1/TaskItems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskItemDto updateDto)
    {
        var existing = await _taskService.GetTaskAsync(id);
        if (existing is null) return NotFound();

        // Apply incoming changes
        existing.Title = updateDto.Title;
        existing.Description = updateDto.Description;
        existing.IsComplete = updateDto.IsComplete;
        existing.StatusId = updateDto.StatusId;

        var validationResult = await _validator.ValidateAsync(existing);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        await _taskService.UpdateTaskAsync(existing);
        return NoContent();
    }

    // DELETE: api/v1/TaskItems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _taskService.GetTaskAsync(id);
        if (existing is null) return NotFound();

        await _taskService.DeleteTaskAsync(id);
        return NoContent();
    }
}
