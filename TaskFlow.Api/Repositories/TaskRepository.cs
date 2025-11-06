using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Repositories;

public class TaskRepository(TaskDbContext context) : ITaskRepository
{
    private readonly TaskDbContext _context = context;

    public async Task<IEnumerable<TaskItem>> GetAllAsync() => 
        await _context.TaskItems
            .Include(t => t.Status)
            .ToListAsync();

    public async Task<TaskItem?> GetByIdAsync(int id) => 
        await _context.TaskItems
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<TaskItem> AddAsync(TaskItem task)
    {
        _context.TaskItems.Add(task);
        await _context.SaveChangesAsync();
        
        // Reload with Status navigation property
        await _context.Entry(task).Reference(t => t.Status).LoadAsync();
        return task;
    }

    public async Task UpdateAsync(TaskItem task)
    {
        _context.TaskItems.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var task = await _context.TaskItems.FindAsync(id);
        if (task == null) return;
        _context.TaskItems.Remove(task);
        await _context.SaveChangesAsync();
    }
}