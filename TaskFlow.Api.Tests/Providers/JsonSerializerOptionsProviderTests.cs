using System.Text.Json;
using FluentAssertions;
using TaskFlow.Api.Providers;

namespace TaskFlow.Api.Tests.Providers;

public class JsonSerializerOptionsProviderTests
{
    [Fact]
    public void Default_ShouldReturnConfiguredOptions()
    {
        var options = JsonSerializerOptionsProvider.Default;

        options.Should().NotBeNull();
        options.WriteIndented.Should().BeTrue();
        options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
    }

    [Fact]
    public void Default_ShouldBeSameInstance_WhenAccessedMultipleTimes()
    {
        var first = JsonSerializerOptionsProvider.Default;
        var second = JsonSerializerOptionsProvider.Default;

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Default_ShouldBeReadOnly()
    {
        var options = JsonSerializerOptionsProvider.Default;

        options.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void ConfigureOptions_ShouldSetWriteIndented()
    {
        var options = new JsonSerializerOptions();

        JsonSerializerOptionsProvider.ConfigureOptions(options);

        options.WriteIndented.Should().BeTrue();
    }

    [Fact]
    public void ConfigureOptions_ShouldSetCamelCaseNamingPolicy()
    {
        var options = new JsonSerializerOptions();

        JsonSerializerOptionsProvider.ConfigureOptions(options);

        options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
    }
}
