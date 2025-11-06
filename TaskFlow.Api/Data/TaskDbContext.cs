using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Data;

public class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Status> Statuses => Set<Status>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Status entity
        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(50);
            entity.Property(s => s.Description).HasMaxLength(200);
            entity.HasIndex(s => s.Name).IsUnique();
        });

        // Configure TaskItem-Status relationship
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasOne(t => t.Status)
                  .WithMany(s => s.TaskItems)
                  .HasForeignKey(t => t.StatusId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed predefined statuses with static DateTime values
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Status>().HasData(
            new Status { Id = 1, Name = "Todo", Description = "Task is pending", CreatedDate = seedDate, UpdatedDate = seedDate },
            new Status { Id = 2, Name = "In Progress", Description = "Task is being worked on", CreatedDate = seedDate, UpdatedDate = seedDate },
            new Status { Id = 3, Name = "Done", Description = "Task is completed", CreatedDate = seedDate, UpdatedDate = seedDate }
        );
    }
}