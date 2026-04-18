using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Repositories;

public class NoteRepository(TaskDbContext context) : INoteRepository
{
    private readonly TaskDbContext _context = context;

    public async Task<IEnumerable<Note>> GetAllByTaskIdAsync(int taskId) =>
        await _context.Notes.Where(n => n.TaskItemId == taskId).ToListAsync();

    public async Task<Note?> GetByIdAsync(int taskId, int noteId) =>
        await _context.Notes.FirstOrDefaultAsync(n => n.Id == noteId && n.TaskItemId == taskId);

    public async Task<Note> AddAsync(Note note)
    {
        note.CreatedAt = DateTime.UtcNow;
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    public async Task UpdateAsync(Note note)
    {
        note.UpdatedAt = DateTime.UtcNow;
        _context.Notes.Update(note);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var note = await _context.Notes.FindAsync(id);
        if (note is null)
        {
            return;
        }

        _context.Notes.Remove(note);
        await _context.SaveChangesAsync();
    }
}
