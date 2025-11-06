using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;

namespace TaskFlow.Api.Tests.Repositories;

public class TaskRepositoryTests
{
    private static TaskDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TaskDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTasks()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TaskRepository(context);
        
        // Create a Status first
        var status = new Status 
        { 
            Id = 1, 
            Name = "Active", 
            Description = "Active tasks",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await context.Statuses.AddAsync(status);
        await context.SaveChangesAsync();
        
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Title = "Task 1", Description = "Description 1", IsComplete = false, StatusId = 1 },
            new() { Id = 2, Title = "Task 2", Description = "Description 2", IsComplete = true, StatusId = 1 }
        };
        await context.TaskItems.AddRangeAsync(tasks);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.Status.Should().NotBeNull());
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoTasks()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TaskRepository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTask_WhenTaskExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TaskRepository(context);
    
        // Create a Status first
        var status = new Status 
        { 
            Id = 1, 
            Name = "Active", 
            Description = "Active tasks",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await context.Statuses.AddAsync(status);
        await context.SaveChangesAsync();
    
        var task = new TaskItem 
        { 
            Id = 1, 
            Title = "Task 1", 
            Description = "Description 1", 
            IsComplete = false,
            StatusId = 1  // <-- Add this
        };
        await context.TaskItems.AddAsync(task);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().NotBeNull();  // <-- Also verify Status is loaded
        result.Title.Should().Be("Task 1");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenTaskDoesNotExist()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TaskRepository(context);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddTaskAndReturnIt()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TaskRepository(context);
        var newTask = new TaskItem { Title = "New Task", Description = "Description", IsComplete = false };

        // Act
        var result = await repository.AddAsync(newTask);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("New Task");

        var savedTask = await context.TaskItems.FindAsync(result.Id);
        savedTask.Should().NotBeNull();
        savedTask.Should().BeEquivalentTo(result);
    }

    [Fact]
    public async Task AddAsync_ShouldHandleTaskWithNullDescription()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TaskRepository(context);
        var newTask = new TaskItem { Title = "Task", Description = null, IsComplete = false };

        // Act
        var result = await repository.AddAsync(newTask);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().BeNull();

        var savedTask = await context.TaskItems.FindAsync(result.Id);
        savedTask.Should().NotBeNull();
        savedTask!.Description.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldHandleCompletedTask()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TaskRepository(context);
        var newTask = new TaskItem { Title = "Completed Task", Description = "Done", IsComplete = true };

        // Act
        var result = await repository.AddAsync(newTask);

        // Assert
        result.Should().NotBeNull();
        result.IsComplete.Should().BeTrue();

        var savedTask = await context.TaskItems.FindAsync(result.Id);
        savedTask.Should().NotBeNull();
        savedTask!.IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingTask()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TaskRepository(context);
        var task = new TaskItem { Id = 1, Title = "Original", Description = "Original Description", IsComplete = false };
        await context.TaskItems.AddAsync(task);
        await context.SaveChangesAsync();

        // Modify the task
        task.Title = "Updated";
        task.Description = "Updated Description";
        task.IsComplete = true;

        // Act
        await repository.UpdateAsync(task);

        // Assert
        var updatedTask = await context.TaskItems.FindAsync(1);
        updatedTask.Should().NotBeNull();
        updatedTask!.Title.Should().Be("Updated");
        updatedTask.Description.Should().Be("Updated Description");
        updatedTask.IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldHandlePartialUpdate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TaskRepository(context);
        var task = new TaskItem { Id = 1, Title = "Original", Description = "Description", IsComplete = false };
        await context.TaskItems.AddAsync(task);
        await context.SaveChangesAsync();

        // Modify only IsComplete
        task.IsComplete = true;

        // Act
        await repository.UpdateAsync(task);

        // Assert
        var updatedTask = await context.TaskItems.FindAsync(1);
        updatedTask.Should().NotBeNull();
        updatedTask!.Title.Should().Be("Original");
        updatedTask.Description.Should().Be("Description");
        updatedTask.IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTask_WhenTaskExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TaskRepository(context);
        var task = new TaskItem { Id = 1, Title = "Task to Delete", Description = "Description", IsComplete = false };
        await context.TaskItems.AddAsync(task);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(1);

        // Assert
        var deletedTask = await context.TaskItems.FindAsync(1);
        deletedTask.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenTaskDoesNotExist()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TaskRepository(context);

        // Act
        var act = async () => await repository.DeleteAsync(999);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_ShouldOnlyDeleteSpecifiedTask()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TaskRepository(context);
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Title = "Task 1", Description = "Description 1", IsComplete = false },
            new() { Id = 2, Title = "Task 2", Description = "Description 2", IsComplete = true }
        };
        await context.TaskItems.AddRangeAsync(tasks);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(1);

        // Assert
        var remainingTasks = await context.TaskItems.ToListAsync();
        remainingTasks.Should().HaveCount(1);
        remainingTasks.First().Id.Should().Be(2);
    }
}
