using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskItemsController : ControllerBase
{
    // In-memory store
    private static readonly List<TaskItem> _items =
    [
        new TaskItem { Id = 1, Title = "Sample Task", Description = "This is a sample task.", IsComplete = false }
    ];

    private static int _nextId = _items.Count != 0 ? _items.Max(i => i.Id) + 1 : 1;
    private static readonly Lock _lock = new();

    // GET: api/TaskItems
    [HttpGet]
    public ActionResult<IEnumerable<TaskItem>> GetAll()
    {
        List<TaskItem> snapshot;
        lock (_lock)
        {
            snapshot = new List<TaskItem>(_items);
        }
        return Ok(snapshot);
    }

    // GET: api/TaskItems/5
    [HttpGet("{id}", Name = "GetTask")]
    public ActionResult<TaskItem> Get(int id)
    {
        var item = _items.FirstOrDefault(t => t.Id == id);
        if (item is null) return NotFound();
        return Ok(item);
    }

    // POST: api/TaskItems
    [HttpPost]
    public ActionResult<TaskItem> Create([FromBody] CreateTaskItemDto createDto)
    {
        var item = new TaskItem
        {
            Title = createDto.Title,
            Description = createDto.Description,
            IsComplete = createDto.IsComplete
        };

        lock (_lock)
        {
            item.Id = _nextId++;
            _items.Add(item);
        }

        return CreatedAtRoute("GetTask", new { id = item.Id }, item);
    }

    // PUT: api/TaskItems/5
    [HttpPut("{id}")]
    public IActionResult Update(int id, [FromBody] UpdateTaskItemDto updateDto)
    {
        lock (_lock)
        {
            var existing = _items.FirstOrDefault(t => t.Id == id);
            if (existing is null) return NotFound();

            // Update fields
            existing.Title = updateDto.Title;
            existing.Description = updateDto.Description;
            existing.IsComplete = updateDto.IsComplete;
        }

        return NoContent();
    }

    // DELETE: api/TaskItems/5
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        lock (_lock)
        {
            var existing = _items.FirstOrDefault(t => t.Id == id);
            if (existing is null) return NotFound();
            _items.Remove(existing);
        }

        return NoContent();
    }
}
