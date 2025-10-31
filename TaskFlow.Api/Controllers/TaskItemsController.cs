using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskItemsController : ControllerBase
{
    private readonly TaskDbContext _db;

    public TaskItemsController(TaskDbContext db)
    {
        _db = db;
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

        _db.TaskItems.Add(item);
        await _db.SaveChangesAsync();

        return CreatedAtRoute("GetTask", new { id = item.Id }, item);
    }

    // PUT: api/TaskItems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskItemDto updateDto)
    {
        if (string.IsNullOrWhiteSpace(updateDto.Title))
        {
            return BadRequest("Title cannot be null, empty, or whitespace.");
        }

        var existing = await _db.TaskItems.FindAsync(id);
        if (existing is null) return NotFound();

        existing.Title = updateDto.Title;
        existing.Description = updateDto.Description;
        existing.IsComplete = updateDto.IsComplete;

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
