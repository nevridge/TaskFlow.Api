using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;

namespace TaskFlow.Api.Tests.Repositories;

public class StatusRepositoryTests
{
    private static TaskDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TaskDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllStatuses()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new StatusRepository(context);
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
        await context.Statuses.AddRangeAsync(statuses);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(statuses);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoStatuses()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new StatusRepository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnStatus_WhenStatusExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new StatusRepository(context);
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

        // Act
        var result = await repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(status);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenStatusDoesNotExist()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new StatusRepository(context);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddStatusAndReturnIt()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new StatusRepository(context);
        var newStatus = new Status
        {
            Name = "In Progress",
            Description = "Tasks in progress",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var result = await repository.AddAsync(newStatus);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("In Progress");

        var savedStatus = await context.Statuses.FindAsync(result.Id);
        savedStatus.Should().NotBeNull();
        savedStatus.Should().BeEquivalentTo(result);
    }

    [Fact]
    public async Task AddAsync_ShouldHandleStatusWithNullDescription()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new StatusRepository(context);
        var newStatus = new Status
        {
            Name = "Pending",
            Description = null,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var result = await repository.AddAsync(newStatus);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().BeNull();

        var savedStatus = await context.Statuses.FindAsync(result.Id);
        savedStatus.Should().NotBeNull();
        savedStatus!.Description.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingStatus()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new StatusRepository(context);
        var status = new Status
        {
            Id = 1,
            Name = "Original",
            Description = "Original Description",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await context.Statuses.AddAsync(status);
        await context.SaveChangesAsync();

        // Modify the status
        status.Name = "Updated";
        status.Description = "Updated Description";
        status.UpdatedDate = DateTime.UtcNow;

        // Act
        await repository.UpdateAsync(status);

        // Assert
        var updatedStatus = await context.Statuses.FindAsync(1);
        updatedStatus.Should().NotBeNull();
        updatedStatus!.Name.Should().Be("Updated");
        updatedStatus.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task UpdateAsync_ShouldHandlePartialUpdate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new StatusRepository(context);
        var status = new Status
        {
            Id = 1,
            Name = "Original",
            Description = "Description",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await context.Statuses.AddAsync(status);
        await context.SaveChangesAsync();

        // Modify only Name
        status.Name = "Modified";

        // Act
        await repository.UpdateAsync(status);

        // Assert
        var updatedStatus = await context.Statuses.FindAsync(1);
        updatedStatus.Should().NotBeNull();
        updatedStatus!.Name.Should().Be("Modified");
        updatedStatus.Description.Should().Be("Description");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveStatus_WhenStatusExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new StatusRepository(context);
        var status = new Status
        {
            Id = 1,
            Name = "To Delete",
            Description = "Description",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await context.Statuses.AddAsync(status);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(1);

        // Assert
        var deletedStatus = await context.Statuses.FindAsync(1);
        deletedStatus.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenStatusDoesNotExist()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new StatusRepository(context);

        // Act
        var act = async () => await repository.DeleteAsync(999);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_ShouldOnlyDeleteSpecifiedStatus()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new StatusRepository(context);
        var statuses = new List<Status>
        {
            new()
            {
                Id = 1,
                Name = "Status 1",
                Description = "Description 1",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Name = "Status 2",
                Description = "Description 2",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            }
        };
        await context.Statuses.AddRangeAsync(statuses);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(1);

        // Assert
        var remainingStatuses = await context.Statuses.ToListAsync();
        remainingStatuses.Should().HaveCount(1);
        remainingStatuses.First().Id.Should().Be(2);
    }
}
