using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;
using TaskFlow.Api.Validators;

namespace TaskFlow.Api.Tests.Validators;

public class StatusValidatorTests
{
    private readonly StatusValidator _validator;
    private readonly TaskDbContext _dbContext;

    public StatusValidatorTests()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TaskDbContext(options);
        _validator = new StatusValidator(_dbContext);
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenStatusIsValid()
    {
        // Arrange
        var status = new Status
        {
            Name = "Active",
            Description = "Active tasks",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var result = await _validator.ValidateAsync(status);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenDescriptionIsNull()
    {
        // Arrange
        var status = new Status
        {
            Name = "Active",
            Description = null,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var result = await _validator.ValidateAsync(status);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenDescriptionIsEmpty()
    {
        // Arrange
        var status = new Status
        {
            Name = "Active",
            Description = string.Empty,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var result = await _validator.ValidateAsync(status);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenNameIsEmpty()
    {
        // Arrange
        var status = new Status
        {
            Name = string.Empty,
            Description = "Description",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var result = await _validator.ValidateAsync(status);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].PropertyName.Should().Be("Name");
        result.Errors[0].ErrorMessage.Should().Be("Status name is required.");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenNameExceedsMaxLength()
    {
        // Arrange
        var status = new Status
        {
            Name = new string('a', 51), // 51 characters
            Description = "Description",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var result = await _validator.ValidateAsync(status);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].PropertyName.Should().Be("Name");
        result.Errors[0].ErrorMessage.Should().Be("Status name cannot exceed 50 characters.");
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenNameIsExactly50Characters()
    {
        // Arrange
        var status = new Status
        {
            Name = new string('a', 50), // Exactly 50 characters
            Description = "Description",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var result = await _validator.ValidateAsync(status);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenDescriptionExceedsMaxLength()
    {
        // Arrange
        var status = new Status
        {
            Name = "Active",
            Description = new string('a', 201), // 201 characters
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var result = await _validator.ValidateAsync(status);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].PropertyName.Should().Be("Description");
        result.Errors[0].ErrorMessage.Should().Be("Status description cannot exceed 200 characters.");
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenDescriptionIsExactly200Characters()
    {
        // Arrange
        var status = new Status
        {
            Name = "Active",
            Description = new string('a', 200), // Exactly 200 characters
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var result = await _validator.ValidateAsync(status);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenDuplicateNameExists()
    {
        // Arrange
        var existingStatus = new Status
        {
            Id = 1,
            Name = "Active",
            Description = "Existing status",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await _dbContext.Statuses.AddAsync(existingStatus);
        await _dbContext.SaveChangesAsync();

        var newStatus = new Status
        {
            Name = "Active", // Duplicate name
            Description = "New status",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var result = await _validator.ValidateAsync(newStatus);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].PropertyName.Should().Be("Name");
        result.Errors[0].ErrorMessage.Should().Be("A status with the same name already exists.");
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenUpdatingStatusWithSameName()
    {
        // Arrange
        var existingStatus = new Status
        {
            Id = 1,
            Name = "Active",
            Description = "Original description",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await _dbContext.Statuses.AddAsync(existingStatus);
        await _dbContext.SaveChangesAsync();

        // Update the same status (same Id, same Name)
        existingStatus.Description = "Updated description";

        // Act
        var result = await _validator.ValidateAsync(existingStatus);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenNameIsUnique()
    {
        // Arrange
        var existingStatus = new Status
        {
            Id = 1,
            Name = "Active",
            Description = "Existing status",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await _dbContext.Statuses.AddAsync(existingStatus);
        await _dbContext.SaveChangesAsync();

        var newStatus = new Status
        {
            Name = "Completed", // Different name
            Description = "New status",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var result = await _validator.ValidateAsync(newStatus);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}