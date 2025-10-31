using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskItemsController : ControllerBase
{
    private readonly TaskDbContext _db;
    private readonly IValidator<TaskItem> _validator;

    public TaskItemsController(TaskDbContext db, IValidator<TaskItem> validator)
    {
        _db = db;
        _validator = validator;
    }

    // GET: api/TaskItems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItem>>> GetAll()
    {
        var items = await _db.TaskItems
            .AsNoTracking()
            .ToListAsync();
        return Ok(items);
    }

    // GET: api/TaskItems/5
    [HttpGet("{id}", Name = "GetTask")]
    public async Task<ActionResult<TaskItem>> Get(int id)
    {
        var item = await _db.TaskItems
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

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

        _db.TaskItems.Add(item);
        await _db.SaveChangesAsync();

        return CreatedAtRoute("GetTask", new { id = item.Id }, item);
    }

    // PUT: api/TaskItems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskItemDto updateDto)
    {
        var existing = await _db.TaskItems.FindAsync(id);
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

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/TaskItems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.TaskItems.FindAsync(id);
        if (existing is null) return NotFound();

        _db.TaskItems.Remove(existing);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
