using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;
using TaskFlow.Api.Validators;

namespace TaskFlow.Api.Tests.Validators;

public class TaskItemValidatorTests
{
    private readonly TaskItemValidator _validator;
    private readonly TaskDbContext _dbContext;

    public TaskItemValidatorTests()
    {
        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use unique DB per test
            .Options;
        _dbContext = new TaskDbContext(options);
        
        // Seed a Status for validation
        SeedStatus();
        
        _validator = new TaskItemValidator(_dbContext);
    }

    private void SeedStatus()
    {
        var status = new Status
        {
            Id = 1,
            Name = "Active",
            Description = "Active tasks",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        _dbContext.Statuses.Add(status);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenTaskIsValid()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Valid Task",
            Description = "Valid Description",
            IsComplete = false,
            StatusId = 1
        };

        // Act
        var result = await _validator.ValidateAsync(task);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenDescriptionIsNull()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Valid Task",
            Description = null,
            IsComplete = false,
            StatusId = 1
        };

        // Act
        var result = await _validator.ValidateAsync(task);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenTaskIsComplete()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Completed Task",
            Description = "Completed",
            IsComplete = true,
            StatusId = 1
        };

        // Act
        var result = await _validator.ValidateAsync(task);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenTitleIsEmpty()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = string.Empty,
            Description = "Description",
            IsComplete = false,
            StatusId = 1
        };

        // Act
        var result = await _validator.ValidateAsync(task);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].PropertyName.Should().Be("Title");
        result.Errors[0].ErrorMessage.Should().Be("Title is required.");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenTitleExceedsMaxLength()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = new string('a', 101), // 101 characters
            Description = "Description",
            IsComplete = false,
            StatusId = 1
        };

        // Act
        var result = await _validator.ValidateAsync(task);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].PropertyName.Should().Be("Title");
        result.Errors[0].ErrorMessage.Should().Be("Title must not exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenTitleIsExactly100Characters()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = new string('a', 100), // Exactly 100 characters
            Description = "Description",
            IsComplete = false,
            StatusId = 1
        };

        // Act
        var result = await _validator.ValidateAsync(task);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenTitleIs1Character()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "A",
            Description = "Description",
            IsComplete = false,
            StatusId = 1
        };

        // Act
        var result = await _validator.ValidateAsync(task);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenDescriptionIsEmpty()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Valid Task",
            Description = string.Empty,
            IsComplete = false,
            StatusId = 1
        };

        // Act
        var result = await _validator.ValidateAsync(task);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenDescriptionIsVeryLong()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Valid Task",
            Description = new string('a', 10000),
            IsComplete = false,
            StatusId = 1
        };

        // Act
        var result = await _validator.ValidateAsync(task);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // Add new tests for StatusId validation
    [Fact]
    public async Task Validate_ShouldFail_WhenStatusIdIsZero()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Valid Task",
            Description = "Valid Description",
            IsComplete = false,
            StatusId = 0  // Invalid - doesn't exist in database
        };

        // Act
        var result = await _validator.ValidateAsync(task);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.Should().Contain(e => e.PropertyName == "StatusId");
        result.Errors.Should().Contain(e => e.ErrorMessage == "StatusId must be a valid status.");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenStatusIdDoesNotExist()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Valid Task",
            Description = "Valid Description",
            IsComplete = false,
            StatusId = 999  // Non-existent
        };

        // Act
        var result = await _validator.ValidateAsync(task);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StatusId");
        result.Errors.Should().Contain(e => e.ErrorMessage == "StatusId must be a valid status.");
    }
}
