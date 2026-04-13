using FluentAssertions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using TaskFlow.Api.Extensions;
using TaskFlow.Api.HealthChecks;

namespace TaskFlow.Api.Tests.Extensions;

public class HealthCheckServiceExtensionsTests
{
    [Fact]
    public void CreateHealthCheckOptions_ShouldSetResponseWriter()
    {
        var options = HealthCheckServiceExtensions.CreateHealthCheckOptions();

        options.ResponseWriter.Should().Be((Func<HttpContext, HealthReport, Task>)HealthCheckResponseWriter.WriteHealthCheckResponse);
    }

    [Fact]
    public void CreateHealthCheckOptions_ShouldConfigureStatusCodes()
    {
        var options = HealthCheckServiceExtensions.CreateHealthCheckOptions();

        options.ResultStatusCodes[HealthStatus.Healthy].Should().Be(StatusCodes.Status200OK);
        options.ResultStatusCodes[HealthStatus.Degraded].Should().Be(StatusCodes.Status200OK);
        options.ResultStatusCodes[HealthStatus.Unhealthy].Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public void CreateReadinessHealthCheckOptions_ShouldIncludeOnlyReadyTaggedChecks()
    {
        var options = HealthCheckServiceExtensions.CreateReadinessHealthCheckOptions();

        options.Predicate.Should().NotBeNull();

        var healthCheck = Mock.Of<IHealthCheck>();
        var readyRegistration = new HealthCheckRegistration("database", healthCheck, null, new[] { "ready" });
        var liveRegistration = new HealthCheckRegistration("self", healthCheck, null, new[] { "live" });

        options.Predicate!(readyRegistration).Should().BeTrue();
        options.Predicate!(liveRegistration).Should().BeFalse();
    }

    [Fact]
    public void CreateLivenessHealthCheckOptions_ShouldIncludeOnlyLiveTaggedChecks()
    {
        var options = HealthCheckServiceExtensions.CreateLivenessHealthCheckOptions();

        options.Predicate.Should().NotBeNull();

        var healthCheck = Mock.Of<IHealthCheck>();
        var liveRegistration = new HealthCheckRegistration("self", healthCheck, null, new[] { "live" });
        var readyRegistration = new HealthCheckRegistration("database", healthCheck, null, new[] { "ready" });

        options.Predicate!(liveRegistration).Should().BeTrue();
        options.Predicate!(readyRegistration).Should().BeFalse();
    }
}
