using FluentAssertions;
using Moq;
using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Tests.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _mockRepo;
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _mockRepo = new Mock<ITaskRepository>();
        _service = new TaskService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetAllTasksAsync_ShouldReturnAllTasks()
    {
        // Arrange
        var expectedTasks = new List<TaskItem>
        {
            new() { Id = 1, Title = "Task 1", Description = "Description 1", IsComplete = false },
            new() { Id = 2, Title = "Task 2", Description = "Description 2", IsComplete = true }
        };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expectedTasks);

        // Act
        var result = await _service.GetAllTasksAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedTasks);
        _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllTasksAsync_ShouldReturnEmptyList_WhenNoTasksExist()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        // Act
        var result = await _service.GetAllTasksAsync();

        // Assert
        result.Should().BeEmpty();
        _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTaskAsync_ShouldReturnTask_WhenTaskExists()
    {
        // Arrange
        var expectedTask = new TaskItem { Id = 1, Title = "Task 1", Description = "Description 1", IsComplete = false };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(expectedTask);

        // Act
        var result = await _service.GetTaskAsync(1);

        // Assert
        result.Should().BeEquivalentTo(expectedTask);
        _mockRepo.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetTaskAsync_ShouldReturnNull_WhenTaskDoesNotExist()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _service.GetTaskAsync(999);

        // Assert
        result.Should().BeNull();
        _mockRepo.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldCreateAndReturnTask()
    {
        // Arrange
        var newTask = new TaskItem { Title = "New Task", Description = "New Description", IsComplete = false };
        var createdTask = new TaskItem { Id = 1, Title = "New Task", Description = "New Description", IsComplete = false };
        _mockRepo.Setup(r => r.AddAsync(newTask)).ReturnsAsync(createdTask);

        // Act
        var result = await _service.CreateTaskAsync(newTask);

        // Assert
        result.Should().BeEquivalentTo(createdTask);
        _mockRepo.Verify(r => r.AddAsync(newTask), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldHandleTaskWithNullDescription()
    {
        // Arrange
        var newTask = new TaskItem { Title = "Task without description", Description = null, IsComplete = false };
        var createdTask = new TaskItem { Id = 1, Title = "Task without description", Description = null, IsComplete = false };
        _mockRepo.Setup(r => r.AddAsync(newTask)).ReturnsAsync(createdTask);

        // Act
        var result = await _service.CreateTaskAsync(newTask);

        // Assert
        result.Should().BeEquivalentTo(createdTask);
        result.Description.Should().BeNull();
        _mockRepo.Verify(r => r.AddAsync(newTask), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_ShouldCallRepositoryUpdate()
    {
        // Arrange
        var taskToUpdate = new TaskItem { Id = 1, Title = "Updated Task", Description = "Updated Description", IsComplete = true };
        _mockRepo.Setup(r => r.UpdateAsync(taskToUpdate)).Returns(Task.CompletedTask);

        // Act
        await _service.UpdateTaskAsync(taskToUpdate);

        // Assert
        _mockRepo.Verify(r => r.UpdateAsync(taskToUpdate), Times.Once);
    }

    [Fact]
    public async Task DeleteTaskAsync_ShouldCallRepositoryDelete()
    {
        // Arrange
        var taskIdToDelete = 1;
        _mockRepo.Setup(r => r.DeleteAsync(taskIdToDelete)).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteTaskAsync(taskIdToDelete);

        // Assert
        _mockRepo.Verify(r => r.DeleteAsync(taskIdToDelete), Times.Once);
    }

    [Fact]
    public async Task DeleteTaskAsync_ShouldHandleNonExistentId()
    {
        // Arrange
        var nonExistentId = 999;
        _mockRepo.Setup(r => r.DeleteAsync(nonExistentId)).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteTaskAsync(nonExistentId);

        // Assert
        _mockRepo.Verify(r => r.DeleteAsync(nonExistentId), Times.Once);
    }
}
