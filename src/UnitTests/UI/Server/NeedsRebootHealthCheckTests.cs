using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class NeedsRebootHealthCheckTests
{
    private StubHttpContextAccessor _httpContextAccessor = null!;
    private NeedsRebootHealthCheck _healthCheck = null!;

    [SetUp]
    public void SetUp()
    {
        NeedsRebootHealthCheck.NeedsReboot = false;
        _httpContextAccessor = new StubHttpContextAccessor();
        _healthCheck = CreateHealthCheck(testSupportEnabled: false);
    }

    [TearDown]
    public void TearDown()
    {
        NeedsRebootHealthCheck.NeedsReboot = false;
    }

    [Test]
    public async Task CheckHealthAsync_WhenNeedsRebootFalse_ReturnsHealthy()
    {
        NeedsRebootHealthCheck.NeedsReboot = false;

        var result = await _healthCheck.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Test]
    public async Task CheckHealthAsync_WhenNeedsRebootTrue_ReturnsUnhealthy()
    {
        NeedsRebootHealthCheck.NeedsReboot = true;

        var result = await _healthCheck.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("memory is corrupted. Restart process");
    }

    [Test]
    public async Task CheckHealthAsync_WhenTestSupportEnabled_IgnoresProcessWideFlag()
    {
        NeedsRebootHealthCheck.NeedsReboot = true;
        _healthCheck = CreateHealthCheck(testSupportEnabled: true);

        var result = await _healthCheck.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Test]
    public async Task CheckHealthAsync_WhenTestSupportHeaderTrue_ReturnsUnhealthy()
    {
        _healthCheck = CreateHealthCheck(testSupportEnabled: true, secret: "pool-secret");
        _httpContextAccessor.HttpContext = CreateRequestContext(
            secret: "pool-secret",
            needsReboot: "true");

        var result = await _healthCheck.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("memory is corrupted. Restart process");
    }

    [Test]
    public async Task CheckHealthAsync_WhenTestSupportHeaderWithoutSecret_ReturnsHealthy()
    {
        _healthCheck = CreateHealthCheck(testSupportEnabled: true, secret: "pool-secret");
        _httpContextAccessor.HttpContext = CreateRequestContext(
            secret: null,
            needsReboot: "true");

        var result = await _healthCheck.CheckHealthAsync(CreateContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    private NeedsRebootHealthCheck CreateHealthCheck(bool testSupportEnabled, string secret = "unused") =>
        new(
            NullLogger<NeedsRebootHealthCheck>.Instance,
            _httpContextAccessor,
            new TestSupportOptions(testSupportEnabled, secret));

    private HealthCheckContext CreateContext() =>
        new()
        {
            Registration = new HealthCheckRegistration("NeedsReboot", _healthCheck, null, null)
        };

    private static DefaultHttpContext CreateRequestContext(string? secret, string? needsReboot)
    {
        var context = new DefaultHttpContext();
        if (secret is not null)
        {
            context.Request.Headers[TestSupportConstants.SecretHeaderName] = secret;
        }

        if (needsReboot is not null)
        {
            context.Request.Headers[TestSupportConstants.NeedsRebootHeaderName] = needsReboot;
        }

        return context;
    }

    private sealed class StubHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
