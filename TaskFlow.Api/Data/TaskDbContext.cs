using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Data;

public class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.Property(t => t.Status)
                  .HasConversion<int>();
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasOne(n => n.TaskItem)
                  .WithMany(t => t.Notes)
                  .HasForeignKey(n => n.TaskItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
