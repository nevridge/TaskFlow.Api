using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Extensions;

namespace TaskFlow.Api.Tests.Extensions;

public class CorsServiceExtensionsTests
{
    [Fact]
    public void AddCorsPolicy_ShouldRegisterCorsServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Cors:AllowedOrigins:0", "http://localhost:5173" }
            })
            .Build();

        services.AddCorsPolicy(configuration);

        services.Should().Contain(s => s.ServiceType == typeof(ICorsService));
    }

    [Fact]
    public void AddCorsPolicy_ShouldReturnServiceCollection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Cors:AllowedOrigins:0", "http://localhost:5173" }
            })
            .Build();

        var result = services.AddCorsPolicy(configuration);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddCorsPolicy_WithNoOrigins_ShouldStillRegisterCorsServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCorsPolicy(configuration);

        services.Should().Contain(s => s.ServiceType == typeof(ICorsService));
    }
}
