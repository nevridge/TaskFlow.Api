using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskFlow.Api.Controllers.V1;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Tests.Controllers.V1;

public class TaskItemsControllerV1Tests
{
    private readonly Mock<ITaskService> _mockService;
    private readonly Mock<IValidator<TaskItem>> _mockValidator;
    private readonly TaskItemsController _controller;

    public TaskItemsControllerV1Tests()
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
            new() { Id = 1, Title = "Task 1", Description = "Description 1", IsComplete = false, StatusId = 1, Status = new Status { Id = 1, Name = "Todo" } },
            new() { Id = 2, Title = "Task 2", Description = "Description 2", IsComplete = true, StatusId = 2, Status = new Status { Id = 2, Name = "Done" } }
        };
        _mockService.Setup(s => s.GetAllTasksAsync()).ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dtos = okResult.Value.Should().BeAssignableTo<IEnumerable<TaskItemResponseDto>>().Subject;
        dtos.Should().HaveCount(2);
        dtos.Should().Contain(d => d.Id == 1 && d.Title == "Task 1" && d.StatusName == "Todo");
        dtos.Should().Contain(d => d.Id == 2 && d.Title == "Task 2" && d.StatusName == "Done");
        _mockService.Verify(s => s.GetAllTasksAsync(), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldReturnOkWithTask_WhenTaskExists()
    {
        // Arrange
        var task = new TaskItem 
        { 
            Id = 1, 
            Title = "Task 1", 
            Description = "Description", 
            IsComplete = false, 
            StatusId = 1,
            Status = new Status { Id = 1, Name = "Todo" }
        };
        _mockService.Setup(s => s.GetTaskAsync(1)).ReturnsAsync(task);

        // Act
        var result = await _controller.Get(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<TaskItemResponseDto>().Subject;
        dto.Id.Should().Be(1);
        dto.Title.Should().Be("Task 1");
        dto.Description.Should().Be("Description");
        dto.IsComplete.Should().BeFalse();
        dto.StatusName.Should().Be("Todo");
        _mockService.Verify(s => s.GetTaskAsync(1), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetTaskAsync(1)).ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _controller.Get(1);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _mockService.Verify(s => s.GetTaskAsync(1), Times.Once);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtRoute_WhenValid()
    {
        // Arrange
        var createDto = new CreateTaskItemDto
        {
            Title = "New Task",
            Description = "New Description",
            IsComplete = false,
            StatusId = 1
        };
        var createdTask = new TaskItem
        {
            Id = 1,
            Title = createDto.Title,
            Description = createDto.Description,
            IsComplete = createDto.IsComplete,
            StatusId = createDto.StatusId
        };
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<TaskItem>(), default))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.CreateTaskAsync(It.IsAny<TaskItem>()))
            .ReturnsAsync(createdTask);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        createdResult.RouteName.Should().Be("GetTaskV1");
        createdResult.RouteValues.Should().ContainKey("version").WhoseValue.Should().Be("1.0");
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(1);
        createdResult.Value.Should().BeEquivalentTo(createdTask);
    }

    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenValid()
    {
        // Arrange
        var updateDto = new UpdateTaskItemDto
        {
            Title = "Updated Task",
            Description = "Updated Description",
            IsComplete = true,
            StatusId = 2
        };
        var existingTask = new TaskItem { Id = 1, Title = "Old Task", Description = "Old", IsComplete = false, StatusId = 1 };
        _mockService.Setup(s => s.GetTaskAsync(1)).ReturnsAsync(existingTask);
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<TaskItem>(), default))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.UpdateTaskAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Update(1, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockService.Verify(s => s.UpdateTaskAsync(It.IsAny<TaskItem>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenTaskExists()
    {
        // Arrange
        var task = new TaskItem { Id = 1, Title = "Task 1", Description = "Description", IsComplete = false, StatusId = 1 };
        _mockService.Setup(s => s.GetTaskAsync(1)).ReturnsAsync(task);
        _mockService.Setup(s => s.DeleteTaskAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockService.Verify(s => s.DeleteTaskAsync(1), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetTaskAsync(1)).ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _mockService.Verify(s => s.DeleteTaskAsync(It.IsAny<int>()), Times.Never);
    }
}
