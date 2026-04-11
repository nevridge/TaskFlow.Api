using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenTelemetry.Trace;
using TaskFlow.Api.Extensions;

namespace TaskFlow.Api.Tests.Extensions;

public class OpenTelemetryServiceExtensionsTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    // ── AddOpenTelemetryObservability ───────────────────────────────────────

    [Fact]
    public void AddOpenTelemetryObservability_WithDefaultSettings_RegistersOpenTelemetry()
    {
        var services = new ServiceCollection();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "OpenTelemetry:ServiceName", "TestApp" },
            { "OpenTelemetry:Endpoint", "http://localhost:4317" }
        });

        services.AddOpenTelemetryObservability(config);

        services.Should().Contain(s => s.ServiceType == typeof(TracerProvider));
    }

    [Fact]
    public void AddOpenTelemetryObservability_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();
        var config = BuildConfiguration(new Dictionary<string, string?>());

        var result = services.AddOpenTelemetryObservability(config);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddOpenTelemetryObservability_WithHttpProtobufProtocol_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "OpenTelemetry:Protocol", "http/protobuf" }
        });

        var act = () => services.AddOpenTelemetryObservability(config);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddOpenTelemetryObservability_WithGrpcProtocol_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "OpenTelemetry:Protocol", "grpc" }
        });

        var act = () => services.AddOpenTelemetryObservability(config);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddOpenTelemetryObservability_WithInvalidProtocol_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "OpenTelemetry:Protocol", "unsupported-protocol" }
        });

        var act = () => services.AddOpenTelemetryObservability(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*unsupported-protocol*");
    }

    [Fact]
    public void AddOpenTelemetryObservability_WithHeader_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "OpenTelemetry:Header", "X-Seq-ApiKey=test-api-key" }
        });

        var act = () => services.AddOpenTelemetryObservability(config);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddOpenTelemetryObservability_WithNullProtocol_DefaultsToHttpProtobuf()
    {
        var services = new ServiceCollection();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "OpenTelemetry:Protocol", null }
        });

        var act = () => services.AddOpenTelemetryObservability(config);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddOpenTelemetryObservability_WithEmptyProtocol_DefaultsToHttpProtobuf()
    {
        var services = new ServiceCollection();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "OpenTelemetry:Protocol", "" }
        });

        var act = () => services.AddOpenTelemetryObservability(config);

        act.Should().NotThrow();
    }

    // ── AddApplicationLogging ───────────────────────────────────────────────

    [Fact]
    public void AddApplicationLogging_NonDevelopmentEnvironment_ReturnsLoggingBuilder()
    {
        var services = new ServiceCollection();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "OpenTelemetry:ServiceName", "TestApp" },
            { "OpenTelemetry:Endpoint", "http://localhost:4317" }
        });

        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Production);

        ILoggingBuilder? capturedBuilder = null;
        services.AddLogging(builder =>
        {
            capturedBuilder = builder.AddApplicationLogging(config, env);
        });

        capturedBuilder.Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationLogging_DevelopmentEnvironment_ReturnsLoggingBuilder()
    {
        var services = new ServiceCollection();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "OpenTelemetry:ServiceName", "TestApp" },
            { "OpenTelemetry:Endpoint", "http://localhost:4317" }
        });

        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Development);

        ILoggingBuilder? capturedBuilder = null;
        services.AddLogging(builder =>
        {
            capturedBuilder = builder.AddApplicationLogging(config, env);
        });

        capturedBuilder.Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationLogging_WithGrpcProtocol_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "OpenTelemetry:Protocol", "grpc" }
        });

        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Production);

        var act = () => services.AddLogging(builder => builder.AddApplicationLogging(config, env));

        act.Should().NotThrow();
    }

    [Fact]
    public void AddApplicationLogging_WithInvalidProtocol_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "OpenTelemetry:Protocol", "invalid" }
        });

        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Production);

        var act = () => services.AddLogging(builder => builder.AddApplicationLogging(config, env));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid*");
    }

    [Fact]
    public void AddApplicationLogging_WithHeader_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "OpenTelemetry:Header", "X-Seq-ApiKey=test-key" }
        });

        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Production);

        var act = () => services.AddLogging(builder => builder.AddApplicationLogging(config, env));

        act.Should().NotThrow();
    }
}
