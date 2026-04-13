using FluentAssertions;
using Moq;
using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Tests.Services;

public class StatusServiceTests
{
    private readonly Mock<IStatusRepository> _mockRepository;
    private readonly StatusService _service;

    public StatusServiceTests()
    {
        _mockRepository = new Mock<IStatusRepository>();
        _service = new StatusService(_mockRepository.Object);
    }

    [Fact]
    public async Task GetAllStatusesAsync_ShouldReturnAllStatuses()
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
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(statuses);

        // Act
        var result = await _service.GetAllStatusesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(statuses);
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllStatusesAsync_ShouldReturnEmptyList_WhenNoStatuses()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        // Act
        var result = await _service.GetAllStatusesAsync();

        // Assert
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnStatus_WhenFound()
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
        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(status);

        // Act
        var result = await _service.GetStatusAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(status);
        _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Status?)null);

        // Act
        var result = await _service.GetStatusAsync(999);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task CreateStatusAsync_ShouldCreateAndReturnStatus()
    {
        // Arrange
        var newStatus = new Status
        {
            Name = "In Progress",
            Description = "Tasks in progress",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        var createdStatus = new Status
        {
            Id = 1,
            Name = "In Progress",
            Description = "Tasks in progress",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        _mockRepository.Setup(r => r.AddAsync(newStatus)).ReturnsAsync(createdStatus);

        // Act
        var result = await _service.CreateStatusAsync(newStatus);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("In Progress");
        _mockRepository.Verify(r => r.AddAsync(newStatus), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldCallRepositoryUpdate()
    {
        // Arrange
        var status = new Status
        {
            Id = 1,
            Name = "Updated",
            Description = "Updated Description",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        _mockRepository.Setup(r => r.UpdateAsync(status)).Returns(Task.CompletedTask);

        // Act
        await _service.UpdateStatusAsync(status);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(status), Times.Once);
    }

    [Fact]
    public async Task DeleteStatusAsync_ShouldCallRepositoryDelete()
    {
        // Arrange
        var statusId = 1;
        _mockRepository.Setup(r => r.DeleteAsync(statusId)).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteStatusAsync(statusId);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(statusId), Times.Once);
    }
}
