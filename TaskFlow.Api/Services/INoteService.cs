using TaskFlow.Api.Models;

namespace TaskFlow.Api.Services;

public interface INoteService
{
    Task<IEnumerable<Note>> GetNotesForTaskAsync(int taskId);
    Task<Note?> GetNoteAsync(int taskId, int noteId);
    Task<Note> CreateNoteAsync(Note note);
    Task UpdateNoteAsync(Note note);
    Task DeleteNoteAsync(int id);
}
