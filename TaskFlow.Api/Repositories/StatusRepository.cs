using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Repositories;

public class StatusRepository(TaskDbContext context) : IStatusRepository
{
    private readonly TaskDbContext _context = context;

    public async Task<Status> AddAsync(Status status)
    {
        _context.Statuses.Add(status);
        await _context.SaveChangesAsync();

        // Reload to ensure all properties are populated
        var reloaded = await _context.Statuses.FindAsync(status.Id);
        return reloaded ?? status;
    }

    public async Task DeleteAsync(int id)
    {
        var status = await _context.Statuses.FindAsync(id);
        if (status == null) return;
        _context.Statuses.Remove(status);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Status>> GetAllAsync() =>
        await _context.Statuses
            .ToListAsync();

    public async Task<Status?> GetByIdAsync(int id) => await _context.Statuses
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task UpdateAsync(Status status)
    {
        _context.Statuses.Update(status);
        await _context.SaveChangesAsync();
    }
}
