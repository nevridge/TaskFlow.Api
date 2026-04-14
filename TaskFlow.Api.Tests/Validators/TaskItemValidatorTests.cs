using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;
using TaskFlow.Api.Validators;

namespace TaskFlow.Api.Tests.Validators;

public class TaskItemValidatorTests
{
    private readonly TaskItemValidator _validator;

    public TaskItemValidatorTests()
    {
        _validator = new TaskItemValidator();
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
            Status = Status.Todo
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
            Status = Status.Todo
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
            Status = Status.Completed
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
            Status = Status.Todo
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
            Status = Status.Todo
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
            Status = Status.Todo
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
            Status = Status.Todo
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
            Status = Status.Todo
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
            Status = Status.Todo
        };

        // Act
        var result = await _validator.ValidateAsync(task);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenStatusIsDraft()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Draft Task",
            Description = "Description",
            IsComplete = false,
            Status = Status.Draft
        };

        // Act
        var result = await _validator.ValidateAsync(task);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenStatusIsCompleted()
    {
        // Arrange
        var task = new TaskItem
        {
            Title = "Completed Task",
            Description = "Description",
            IsComplete = true,
            Status = Status.Completed
        };

        // Act
        var result = await _validator.ValidateAsync(task);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
