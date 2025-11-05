using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Configuration;

namespace TaskFlow.Api.Tests.Configuration;

public class ApiVersioningServiceExtensionsTests
{
    [Fact]
    public void AddApiVersioningConfiguration_ShouldRegisterApiVersioningServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddApiVersioningConfiguration();

        // Assert
        result.Should().BeSameAs(services);
        
        // Verify that API versioning services are registered by checking the service collection
        services.Should().Contain(s => s.ServiceType.FullName != null && 
                                       s.ServiceType.FullName.Contains("ApiVersion"));
    }

    [Fact]
    public void AddApiVersioningConfiguration_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddApiVersioningConfiguration();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ServiceCollection>();
    }
}
