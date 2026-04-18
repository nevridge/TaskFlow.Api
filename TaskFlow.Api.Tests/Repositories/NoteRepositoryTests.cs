using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;

namespace TaskFlow.Api.Tests.Repositories;

public class NoteRepositoryTests
{
    private static TaskDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TaskDbContext(options);
    }

    private static TaskItem SeedTask(TaskDbContext context, int id = 1)
    {
        var task = new TaskItem { Id = id, Title = $"Task {id}" };
        context.TaskItems.Add(task);
        context.SaveChanges();
        return task;
    }

    [Fact]
    public async Task GetAllByTaskIdAsync_ShouldReturnNotesForTask()
    {
        using var context = CreateInMemoryContext();
        SeedTask(context, 1);
        SeedTask(context, 2);
        context.Notes.AddRange(
            new Note { Content = "Note A", TaskItemId = 1 },
            new Note { Content = "Note B", TaskItemId = 1 },
            new Note { Content = "Note C", TaskItemId = 2 }
        );
        await context.SaveChangesAsync();
        var repo = new NoteRepository(context);

        var result = await repo.GetAllByTaskIdAsync(1);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(n => n.TaskItemId.Should().Be(1));
    }

    [Fact]
    public async Task GetAllByTaskIdAsync_ShouldReturnEmpty_WhenTaskHasNoNotes()
    {
        using var context = CreateInMemoryContext();
        SeedTask(context, 1);
        var repo = new NoteRepository(context);

        var result = await repo.GetAllByTaskIdAsync(1);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNote_WhenNoteExistsForTask()
    {
        using var context = CreateInMemoryContext();
        SeedTask(context, 1);
        var note = new Note { Content = "Test note", TaskItemId = 1 };
        context.Notes.Add(note);
        await context.SaveChangesAsync();
        var repo = new NoteRepository(context);

        var result = await repo.GetByIdAsync(1, note.Id);

        result.Should().NotBeNull();
        result!.Content.Should().Be("Test note");
        result.TaskItemId.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNoteDoesNotBelongToTask()
    {
        using var context = CreateInMemoryContext();
        SeedTask(context, 1);
        SeedTask(context, 2);
        var note = new Note { Content = "Task 2 note", TaskItemId = 2 };
        context.Notes.Add(note);
        await context.SaveChangesAsync();
        var repo = new NoteRepository(context);

        var result = await repo.GetByIdAsync(1, note.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNoteDoesNotExist()
    {
        using var context = CreateInMemoryContext();
        SeedTask(context, 1);
        var repo = new NoteRepository(context);

        var result = await repo.GetByIdAsync(1, 999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddNoteAndReturnIt()
    {
        using var context = CreateInMemoryContext();
        SeedTask(context, 1);
        var repo = new NoteRepository(context);
        var note = new Note { Content = "New note", TaskItemId = 1 };

        var result = await repo.AddAsync(note);

        result.Id.Should().BeGreaterThan(0);
        result.Content.Should().Be("New note");
        result.TaskItemId.Should().Be(1);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var saved = await context.Notes.FindAsync(result.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateNoteContent()
    {
        using var context = CreateInMemoryContext();
        SeedTask(context, 1);
        var note = new Note { Content = "Original", TaskItemId = 1 };
        context.Notes.Add(note);
        await context.SaveChangesAsync();
        var repo = new NoteRepository(context);

        note.Content = "Updated";
        await repo.UpdateAsync(note);

        var updated = await context.Notes.FindAsync(note.Id);
        updated!.Content.Should().Be("Updated");
        updated.UpdatedAt.Should().NotBeNull();
        updated.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveNote_WhenNoteExists()
    {
        using var context = CreateInMemoryContext();
        SeedTask(context, 1);
        var note = new Note { Content = "To delete", TaskItemId = 1 };
        context.Notes.Add(note);
        await context.SaveChangesAsync();
        var repo = new NoteRepository(context);

        await repo.DeleteAsync(note.Id);

        var deleted = await context.Notes.FindAsync(note.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenNoteDoesNotExist()
    {
        using var context = CreateInMemoryContext();
        var repo = new NoteRepository(context);

        var act = async () => await repo.DeleteAsync(999);

        await act.Should().NotThrowAsync();
    }
}
