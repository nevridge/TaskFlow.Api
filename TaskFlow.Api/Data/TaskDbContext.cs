using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Data;

public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options) { }

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
}

