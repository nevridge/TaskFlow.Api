using FluentAssertions;
using Moq;
using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Tests.Services;

public class NoteServiceTests
{
    private readonly Mock<INoteRepository> _mockRepo;
    private readonly NoteService _service;

    public NoteServiceTests()
    {
        _mockRepo = new Mock<INoteRepository>();
        _service = new NoteService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetNotesForTaskAsync_ShouldReturnNotes()
    {
        var notes = new List<Note>
        {
            new() { Id = 1, Content = "Note 1", TaskItemId = 1 },
            new() { Id = 2, Content = "Note 2", TaskItemId = 1 }
        };
        _mockRepo.Setup(r => r.GetAllByTaskIdAsync(1)).ReturnsAsync(notes);

        var result = await _service.GetNotesForTaskAsync(1);

        result.Should().BeEquivalentTo(notes);
        _mockRepo.Verify(r => r.GetAllByTaskIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetNoteAsync_ShouldReturnNote_WhenNoteExists()
    {
        var note = new Note { Id = 1, Content = "Note 1", TaskItemId = 1 };
        _mockRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(note);

        var result = await _service.GetNoteAsync(1, 1);

        result.Should().BeEquivalentTo(note);
        _mockRepo.Verify(r => r.GetByIdAsync(1, 1), Times.Once);
    }

    [Fact]
    public async Task GetNoteAsync_ShouldReturnNull_WhenNoteDoesNotExist()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(1, 999)).ReturnsAsync((Note?)null);

        var result = await _service.GetNoteAsync(1, 999);

        result.Should().BeNull();
        _mockRepo.Verify(r => r.GetByIdAsync(1, 999), Times.Once);
    }

    [Fact]
    public async Task CreateNoteAsync_ShouldCreateAndReturnNote()
    {
        var note = new Note { Content = "New note", TaskItemId = 1 };
        var created = new Note { Id = 1, Content = "New note", TaskItemId = 1 };
        _mockRepo.Setup(r => r.AddAsync(note)).ReturnsAsync(created);

        var result = await _service.CreateNoteAsync(note);

        result.Should().BeEquivalentTo(created);
        _mockRepo.Verify(r => r.AddAsync(note), Times.Once);
    }

    [Fact]
    public async Task UpdateNoteAsync_ShouldCallRepositoryUpdate()
    {
        var note = new Note { Id = 1, Content = "Updated", TaskItemId = 1 };
        _mockRepo.Setup(r => r.UpdateAsync(note)).Returns(Task.CompletedTask);

        await _service.UpdateNoteAsync(note);

        _mockRepo.Verify(r => r.UpdateAsync(note), Times.Once);
    }

    [Fact]
    public async Task DeleteNoteAsync_ShouldCallRepositoryDelete()
    {
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        await _service.DeleteNoteAsync(1);

        _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}
