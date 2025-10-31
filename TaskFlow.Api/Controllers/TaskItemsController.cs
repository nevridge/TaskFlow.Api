using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskItemsController(TaskService taskService, IValidator<TaskItem> validator) : ControllerBase
{
    private readonly TaskService _taskService = taskService;
    private readonly IValidator<TaskItem> _validator = validator;

    // GET: api/TaskItems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItem>>> GetAll()
    {
        var items = await _taskService.GetAllTasksAsync();
        return Ok(items);
    }

    // GET: api/TaskItems/5
    [HttpGet("{id}", Name = "GetTask")]
    public async Task<ActionResult<TaskItem>> Get(int id)
    {
        var item = await _taskService.GetTaskAsync(id);

        if (item is null) return NotFound();
        return Ok(item);
    }

    // POST: api/TaskItems
    [HttpPost]
    public async Task<ActionResult<TaskItem>> Create([FromBody] CreateTaskItemDto createDto)
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

        return CreatedAtRoute("GetTask", new { id = createdItem.Id }, createdItem);
    }

    // PUT: api/TaskItems/5
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

    // DELETE: api/TaskItems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _taskService.GetTaskAsync(id);
        if (existing is null) return NotFound();

        await _taskService.DeleteTaskAsync(id);

        return NoContent();
    }
}
