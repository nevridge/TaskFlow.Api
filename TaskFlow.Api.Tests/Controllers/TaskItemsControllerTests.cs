using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskFlow.Api.Controllers;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Tests.Controllers;

public class TaskItemsControllerTests
{
    private readonly Mock<ITaskService> _mockService;
    private readonly Mock<IValidator<TaskItem>> _mockValidator;
    private readonly TaskItemsController _controller;

    public TaskItemsControllerTests()
    {
        _mockService = new Mock<ITaskService>();
        _mockValidator = new Mock<IValidator<TaskItem>>();
        _controller = new TaskItemsController(_mockService.Object, _mockValidator.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithAllTasks()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Title = "Task 1", Description = "Description 1", IsComplete = false },
            new() { Id = 2, Title = "Task 2", Description = "Description 2", IsComplete = true }
        };
        _mockService.Setup(s => s.GetAllTasksAsync()).ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(tasks);
        _mockService.Verify(s => s.GetAllTasksAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithEmptyList_WhenNoTasks()
    {
        // Arrange
        _mockService.Setup(s => s.GetAllTasksAsync()).ReturnsAsync(new List<TaskItem>());

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var tasks = okResult.Value.Should().BeAssignableTo<IEnumerable<TaskItem>>().Subject;
        tasks.Should().BeEmpty();
        _mockService.Verify(s => s.GetAllTasksAsync(), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldReturnOkWithTask_WhenTaskExists()
    {
        // Arrange
        var task = new TaskItem { Id = 1, Title = "Task 1", Description = "Description 1", IsComplete = false };
        _mockService.Setup(s => s.GetTaskAsync(1)).ReturnsAsync(task);

        // Act
        var result = await _controller.Get(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(task);
        _mockService.Verify(s => s.GetTaskAsync(1), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetTaskAsync(999)).ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _controller.Get(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _mockService.Verify(s => s.GetTaskAsync(999), Times.Once);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtRoute_WhenValidTask()
    {
        // Arrange
        var createDto = new CreateTaskItemDto { Title = "New Task", Description = "Description", IsComplete = false };
        var createdTask = new TaskItem { Id = 1, Title = "New Task", Description = "Description", IsComplete = false };
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<TaskItem>(), default))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.CreateTaskAsync(It.IsAny<TaskItem>())).ReturnsAsync(createdTask);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        createdResult.RouteName.Should().Be("GetTask");
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(1);
        createdResult.Value.Should().BeEquivalentTo(createdTask);
        _mockService.Verify(s => s.CreateTaskAsync(It.IsAny<TaskItem>()), Times.Once);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var createDto = new CreateTaskItemDto { Title = "", Description = "Description", IsComplete = false };
        var validationFailures = new List<ValidationFailure>
        {
            new("Title", "Title is required.")
        };
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<TaskItem>(), default))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(validationFailures);
        _mockService.Verify(s => s.CreateTaskAsync(It.IsAny<TaskItem>()), Times.Never);
    }

    [Fact]
    public async Task Create_ShouldHandleTaskWithNullDescription()
    {
        // Arrange
        var createDto = new CreateTaskItemDto { Title = "Task", Description = null, IsComplete = false };
        var createdTask = new TaskItem { Id = 1, Title = "Task", Description = null, IsComplete = false };
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<TaskItem>(), default))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.CreateTaskAsync(It.IsAny<TaskItem>())).ReturnsAsync(createdTask);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        createdResult.Value.Should().BeEquivalentTo(createdTask);
    }

    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenValidTask()
    {
        // Arrange
        var updateDto = new UpdateTaskItemDto { Title = "Updated Task", Description = "Updated Description", IsComplete = true };
        var existingTask = new TaskItem { Id = 1, Title = "Old Task", Description = "Old Description", IsComplete = false };
        _mockService.Setup(s => s.GetTaskAsync(1)).ReturnsAsync(existingTask);
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<TaskItem>(), default))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.UpdateTaskAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockService.Verify(s => s.GetTaskAsync(1), Times.Once);
        _mockService.Verify(s => s.UpdateTaskAsync(It.Is<TaskItem>(t => 
            t.Id == 1 && 
            t.Title == "Updated Task" && 
            t.Description == "Updated Description" && 
            t.IsComplete == true)), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        // Arrange
        var updateDto = new UpdateTaskItemDto { Title = "Updated Task", Description = "Updated Description", IsComplete = true };
        _mockService.Setup(s => s.GetTaskAsync(999)).ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _controller.Update(999, updateDto);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _mockService.Verify(s => s.GetTaskAsync(999), Times.Once);
        _mockService.Verify(s => s.UpdateTaskAsync(It.IsAny<TaskItem>()), Times.Never);
    }

    [Fact]
    public async Task Update_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var updateDto = new UpdateTaskItemDto { Title = "", Description = "Description", IsComplete = true };
        var existingTask = new TaskItem { Id = 1, Title = "Old Task", Description = "Old Description", IsComplete = false };
        var validationFailures = new List<ValidationFailure>
        {
            new("Title", "Title is required.")
        };
        _mockService.Setup(s => s.GetTaskAsync(1)).ReturnsAsync(existingTask);
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<TaskItem>(), default))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(validationFailures);
        _mockService.Verify(s => s.UpdateTaskAsync(It.IsAny<TaskItem>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenTaskExists()
    {
        // Arrange
        var existingTask = new TaskItem { Id = 1, Title = "Task to Delete", Description = "Description", IsComplete = false };
        _mockService.Setup(s => s.GetTaskAsync(1)).ReturnsAsync(existingTask);
        _mockService.Setup(s => s.DeleteTaskAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockService.Verify(s => s.GetTaskAsync(1), Times.Once);
        _mockService.Verify(s => s.DeleteTaskAsync(1), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetTaskAsync(999)).ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _controller.Delete(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _mockService.Verify(s => s.GetTaskAsync(999), Times.Once);
        _mockService.Verify(s => s.DeleteTaskAsync(It.IsAny<int>()), Times.Never);
    }
}
