using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;

namespace TaskFlow.Api.Services;

public class NoteService(INoteRepository repo) : INoteService
{
    private readonly INoteRepository _repo = repo;

    public async Task<IEnumerable<Note>> GetNotesForTaskAsync(int taskId) =>
        await _repo.GetAllByTaskIdAsync(taskId);

    public async Task<Note?> GetNoteAsync(int taskId, int noteId) =>
        await _repo.GetByIdAsync(taskId, noteId);

    public async Task<Note> CreateNoteAsync(Note note) =>
        await _repo.AddAsync(note);

    public async Task UpdateNoteAsync(Note note) =>
        await _repo.UpdateAsync(note);

    public async Task DeleteNoteAsync(int id) =>
        await _repo.DeleteAsync(id);
}
