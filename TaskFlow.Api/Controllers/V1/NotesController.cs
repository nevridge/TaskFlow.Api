using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/taskitems/{taskId}/notes")]
public class NotesController(INoteService noteService, ITaskService taskService, IValidator<Note> validator) : ControllerBase
{
    private const string GetNoteRouteName = "GetNoteV1";
    private const string ApiVersionString = "1.0";
    private readonly INoteService _noteService = noteService;
    private readonly ITaskService _taskService = taskService;
    private readonly IValidator<Note> _validator = validator;

    // GET: api/v1/taskitems/{taskId}/notes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NoteResponseDto>>> GetAll(int taskId)
    {
        var task = await _taskService.GetTaskAsync(taskId);
        if (task is null)
        {
            return NotFound();
        }

        var notes = await _noteService.GetNotesForTaskAsync(taskId);
        return Ok(notes.Select(ToDto));
    }

    // GET: api/v1/taskitems/{taskId}/notes/{id}
    [HttpGet("{id}", Name = GetNoteRouteName)]
    public async Task<ActionResult<NoteResponseDto>> Get(int taskId, int id)
    {
        var task = await _taskService.GetTaskAsync(taskId);
        if (task is null)
        {
            return NotFound();
        }

        var note = await _noteService.GetNoteAsync(taskId, id);
        if (note is null)
        {
            return NotFound();
        }

        return Ok(ToDto(note));
    }

    // POST: api/v1/taskitems/{taskId}/notes
    [HttpPost]
    public async Task<ActionResult<NoteResponseDto>> Create(int taskId, [FromBody] CreateNoteDto createDto)
    {
        var task = await _taskService.GetTaskAsync(taskId);
        if (task is null)
        {
            return NotFound();
        }

        var note = new Note { Content = createDto.Content, TaskItemId = taskId };

        var validationResult = await _validator.ValidateAsync(note);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var created = await _noteService.CreateNoteAsync(note);
        return CreatedAtRoute(GetNoteRouteName, new { version = ApiVersionString, taskId, id = created.Id }, ToDto(created));
    }

    // PUT: api/v1/taskitems/{taskId}/notes/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<NoteResponseDto>> Update(int taskId, int id, [FromBody] UpdateNoteDto updateDto)
    {
        var task = await _taskService.GetTaskAsync(taskId);
        if (task is null)
        {
            return NotFound();
        }

        var existing = await _noteService.GetNoteAsync(taskId, id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Content = updateDto.Content;

        var validationResult = await _validator.ValidateAsync(existing);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        await _noteService.UpdateNoteAsync(existing);
        return Ok(ToDto(existing));
    }

    // DELETE: api/v1/taskitems/{taskId}/notes/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int taskId, int id)
    {
        var task = await _taskService.GetTaskAsync(taskId);
        if (task is null)
        {
            return NotFound();
        }

        var existing = await _noteService.GetNoteAsync(taskId, id);
        if (existing is null)
        {
            return NotFound();
        }

        await _noteService.DeleteNoteAsync(id);
        return NoContent();
    }

    private static NoteResponseDto ToDto(Note note) => new()
    {
        Id = note.Id,
        Content = note.Content,
        TaskItemId = note.TaskItemId,
        CreatedAt = note.CreatedAt,
        UpdatedAt = note.UpdatedAt
    };
}
