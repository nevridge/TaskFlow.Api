using FluentAssertions;
using NSubstitute;
using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Tests.Services;

public class StatusServiceTests
{
    private readonly IStatusRepository _mockRepository;
    private readonly StatusService _service;

    public StatusServiceTests()
    {
        _mockRepository = Substitute.For<IStatusRepository>();
        _service = new StatusService(_mockRepository);
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
        _mockRepository.GetAllAsync().Returns(statuses);

        // Act
        var result = await _service.GetAllStatusesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(statuses);
        await _mockRepository.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task GetAllStatusesAsync_ShouldReturnEmptyList_WhenNoStatuses()
    {
        // Arrange
        _mockRepository.GetAllAsync().Returns([]);

        // Act
        var result = await _service.GetAllStatusesAsync();

        // Assert
        result.Should().BeEmpty();
        await _mockRepository.Received(1).GetAllAsync();
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
        _mockRepository.GetByIdAsync(1).Returns(status);

        // Act
        var result = await _service.GetStatusAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(status);
        await _mockRepository.Received(1).GetByIdAsync(1);
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        _mockRepository.GetByIdAsync(999).Returns((Status?)null);

        // Act
        var result = await _service.GetStatusAsync(999);

        // Assert
        result.Should().BeNull();
        await _mockRepository.Received(1).GetByIdAsync(999);
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
        _mockRepository.AddAsync(newStatus).Returns(createdStatus);

        // Act
        var result = await _service.CreateStatusAsync(newStatus);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("In Progress");
        await _mockRepository.Received(1).AddAsync(newStatus);
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

        // Act
        await _service.UpdateStatusAsync(status);

        // Assert
        await _mockRepository.Received(1).UpdateAsync(status);
    }

    [Fact]
    public async Task DeleteStatusAsync_ShouldCallRepositoryDelete()
    {
        // Arrange
        var statusId = 1;

        // Act
        await _service.DeleteStatusAsync(statusId);

        // Assert
        await _mockRepository.Received(1).DeleteAsync(statusId);
    }
}
