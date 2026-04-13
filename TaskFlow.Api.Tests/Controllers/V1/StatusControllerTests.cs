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

public class StatusControllerTests
{
    private readonly Mock<IStatusService> _mockService;
    private readonly Mock<IValidator<Status>> _mockValidator;
    private readonly StatusController _controller;

    public StatusControllerTests()
    {
        _mockService = new Mock<IStatusService>();
        _mockValidator = new Mock<IValidator<Status>>();
        _controller = new StatusController(_mockService.Object, _mockValidator.Object);
    }

    [Fact]
    public async Task GetStatuses_ShouldReturnOkWithStatuses()
    {
        // Arrange
        var statuses = new List<Status>
        {
            new()
            {
                Id = 1,
                Name = "Active",
                Description = "Active tasks",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Name = "Completed",
                Description = "Completed tasks",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            }
        };
        _mockService.Setup(s => s.GetAllStatusesAsync()).ReturnsAsync(statuses);

        // Act
        var result = await _controller.GetStatuses();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStatuses = okResult.Value.Should().BeAssignableTo<IEnumerable<StatusResponseDto>>().Subject;
        returnedStatuses.Should().HaveCount(2);
        returnedStatuses.Should().BeEquivalentTo(new[]
        {
            new StatusResponseDto { Id = 1, Name = "Active", Description = "Active tasks" },
            new StatusResponseDto { Id = 2, Name = "Completed", Description = "Completed tasks" }
        });
    }

    [Fact]
    public async Task GetStatuses_ShouldReturnEmptyList_WhenNoStatuses()
    {
        // Arrange
        _mockService.Setup(s => s.GetAllStatusesAsync()).ReturnsAsync([]);

        // Act
        var result = await _controller.GetStatuses();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStatuses = okResult.Value.Should().BeAssignableTo<IEnumerable<StatusResponseDto>>().Subject;
        returnedStatuses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatus_ShouldReturnOkWithStatus_WhenStatusExists()
    {
        // Arrange
        var status = new Status
        {
            Id = 1,
            Name = "Active",
            Description = "Active tasks",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        _mockService.Setup(s => s.GetStatusAsync(1)).ReturnsAsync(status);

        // Act
        var result = await _controller.GetStatus(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStatus = okResult.Value.Should().BeAssignableTo<StatusResponseDto>().Subject;
        returnedStatus.Should().BeEquivalentTo(new StatusResponseDto
        {
            Id = 1,
            Name = "Active",
            Description = "Active tasks"
        });
    }

    [Fact]
    public async Task GetStatus_ShouldReturnNotFound_WhenStatusDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetStatusAsync(999)).ReturnsAsync((Status?)null);

        // Act
        var result = await _controller.GetStatus(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateStatus_ShouldReturnCreatedAtRoute_WhenValidationPasses()
    {
        // Arrange
        var createDto = new CreateStatusDto
        {
            Name = "In Progress",
            Description = "Tasks in progress"
        };
        var createdStatus = new Status
        {
            Id = 1,
            Name = "In Progress",
            Description = "Tasks in progress",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _mockService.Setup(s => s.CreateStatusAsync(It.IsAny<Status>())).ReturnsAsync(createdStatus);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        createdResult.RouteName.Should().Be("GetStatusV1");
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(1);
        var returnedStatus = createdResult.Value.Should().BeAssignableTo<StatusResponseDto>().Subject;
        returnedStatus.Should().BeEquivalentTo(new StatusResponseDto
        {
            Id = 1,
            Name = "In Progress",
            Description = "Tasks in progress"
        });
    }

    [Fact]
    public async Task CreateStatus_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var createDto = new CreateStatusDto
        {
            Name = "",
            Description = "Description"
        };
        var validationFailures = new List<ValidationFailure>
        {
            new("Name", "Status name is required.")
        };

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(validationFailures);
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturnOkWithUpdatedStatus_WhenValidationPasses()
    {
        // Arrange
        var updateDto = new UpdateStatusDto
        {
            Name = "Updated",
            Description = "Updated description"
        };
        var existingStatus = new Status
        {
            Id = 1,
            Name = "Original",
            Description = "Original description",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _mockService.Setup(s => s.GetStatusAsync(1)).ReturnsAsync(existingStatus);
        _mockService.Setup(s => s.UpdateStatusAsync(It.IsAny<Status>()))
            .Returns(Task.CompletedTask);
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await _controller.UpdateStatus(1, updateDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStatus = okResult.Value.Should().BeAssignableTo<StatusResponseDto>().Subject;
        returnedStatus.Should().BeEquivalentTo(new StatusResponseDto
        {
            Id = 1,
            Name = "Updated",
            Description = "Updated description"
        });
        _mockService.Verify(s => s.UpdateStatusAsync(It.Is<Status>(st =>
            st.Name == "Updated" && st.Description == "Updated description")), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturnNotFound_WhenStatusDoesNotExist()
    {
        // Arrange
        var updateDto = new UpdateStatusDto
        {
            Name = "Updated",
            Description = "Updated description"
        };
        _mockService.Setup(s => s.GetStatusAsync(999)).ReturnsAsync((Status?)null);

        // Act
        var result = await _controller.UpdateStatus(999, updateDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _mockService.Verify(s => s.UpdateStatusAsync(It.IsAny<Status>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var updateDto = new UpdateStatusDto
        {
            Name = "",
            Description = "Description"
        };
        var existingStatus = new Status
        {
            Id = 1,
            Name = "Original",
            Description = "Original description",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        var validationFailures = new List<ValidationFailure>
        {
            new("Name", "Status name is required.")
        };

        _mockService.Setup(s => s.GetStatusAsync(1)).ReturnsAsync(existingStatus);
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        var result = await _controller.UpdateStatus(1, updateDto);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(validationFailures);
        _mockService.Verify(s => s.UpdateStatusAsync(It.IsAny<Status>()), Times.Never);
    }

    [Fact]
    public async Task DeleteStatus_ShouldReturnNoContent_WhenStatusExists()
    {
        // Arrange
        var existingStatus = new Status
        {
            Id = 1,
            Name = "Active",
            Description = "Active tasks",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        _mockService.Setup(s => s.GetStatusAsync(1)).ReturnsAsync(existingStatus);

        // Act
        var result = await _controller.DeleteStatus(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockService.Verify(s => s.DeleteStatusAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteStatus_ShouldReturnNotFound_WhenStatusDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetStatusAsync(999)).ReturnsAsync((Status?)null);

        // Act
        var result = await _controller.DeleteStatus(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _mockService.Verify(s => s.DeleteStatusAsync(It.IsAny<int>()), Times.Never);
    }
}
