using FluentAssertions;
using FluentValidation;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskFlow.Api.Configuration;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;
using TaskFlow.Api.Services;
using TaskFlow.Api.Validators;

namespace TaskFlow.Api.Tests.Configuration;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPersistence_ShouldRegisterDbContextAndRepositories()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" }
            })
            .Build();

        // Act
        services.AddPersistence(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<TaskDbContext>().Should().NotBeNull();
        serviceProvider.GetService<ITaskRepository>().Should().NotBeNull();
        serviceProvider.GetService<ITaskRepository>().Should().BeOfType<TaskRepository>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterBusinessLogicServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" }
            })
            .Build();

        // Act
        services.AddPersistence(configuration); // TaskService depends on ITaskRepository
        services.AddApplicationServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<ITaskService>().Should().NotBeNull();
        serviceProvider.GetService<ITaskService>().Should().BeOfType<TaskService>();
    }

    [Fact]
    public void AddValidation_ShouldRegisterValidators()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
            { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" }
            })
            .Build();

        // Act
        services.AddPersistence(configuration);
        services.AddValidation();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IValidator<TaskItem>>().Should().NotBeNull();
        serviceProvider.GetService<IValidator<Status>>().Should().NotBeNull();
        serviceProvider.GetService<IValidator<TaskItem>>().Should().BeOfType<TaskItemValidator>();
        serviceProvider.GetService<IValidator<Status>>().Should().BeOfType<StatusValidator>();
    }

    [Fact]
    public void AddApplicationHealthChecks_ShouldRegisterHealthChecks()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" }
            })
            .Build();

        // Act
        services.AddLogging(); // HealthCheckService requires logging
        services.AddPersistence(configuration);
        services.AddApplicationHealthChecks();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var healthCheckService = serviceProvider.GetService<HealthCheckService>();
        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public void AddSwagger_ShouldRegisterSwaggerServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSwagger();
        // Assert
        // Swagger services are registered internally
        // We verify by checking if the service collection has the expected services
        services.Should().Contain(s => s.ServiceType.Name.Contains("Swagger"));
    }

    [Fact]
    public void ConfigureJsonSerialization_ShouldRegisterJsonOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.ConfigureJsonSerialization();

        // Assert
        services.Should().Contain(s =>
            s.ServiceType.Name.Contains("JsonOptions") ||
            s.ServiceType.Name.Contains("ConfigureOptions"));
    }

    [Fact]
    public void AddApplicationInsights_ShouldRegisterApplicationInsightsServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationInsights();

        // Assert
        // Verify that Application Insights services are registered
        services.Should().Contain(s => s.ServiceType == typeof(TelemetryConfiguration));
    }

    [Fact]
    public void AddApplicationInsights_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddApplicationInsights();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AllExtensions_ShouldWorkTogether()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" }
            })
            .Build();

        // Act
        services.AddLogging(); // Required for health checks
        services.AddPersistence(configuration);
        services.AddApplicationServices();
        services.AddValidation();
        services.AddApplicationHealthChecks();
        services.AddSwagger();
        services.ConfigureJsonSerialization();

        var serviceProvider = services.BuildServiceProvider();

        // Assert - verify all critical services are registered
        serviceProvider.GetService<TaskDbContext>().Should().NotBeNull();
        serviceProvider.GetService<ITaskRepository>().Should().NotBeNull();
        serviceProvider.GetService<ITaskService>().Should().NotBeNull();
        serviceProvider.GetService<IValidator<TaskItem>>().Should().NotBeNull();
        serviceProvider.GetService<HealthCheckService>().Should().NotBeNull();
    }

    [Fact]
    public void AllExtensionsWithApplicationInsights_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" }
            })
            .Build();

        // Act
        services.AddLogging();
        services.AddPersistence(configuration);
        services.AddApplicationServices();
        services.AddValidation();
        services.AddApplicationHealthChecks();
        services.AddApplicationInsights();
        services.AddSwagger();
        services.ConfigureJsonSerialization();

        // Assert - verify all services are registered in the collection
        // Note: We don't build the service provider for this test because
        // Application Insights has complex dependencies that would require
        // a full hosting environment setup
        services.Should().Contain(s => s.ServiceType == typeof(TaskDbContext));
        services.Should().Contain(s => s.ServiceType == typeof(ITaskRepository));
        services.Should().Contain(s => s.ServiceType == typeof(ITaskService));
        services.Should().Contain(s => s.ServiceType == typeof(IValidator<TaskItem>));
        services.Should().Contain(s => s.ServiceType == typeof(HealthCheckService));
        services.Should().Contain(s => s.ServiceType == typeof(TelemetryConfiguration));
    }
}
