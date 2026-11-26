using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>
/// Pins the API-key gate's Specification (<see cref="ApiRoutePolicy.RequiresApiKey"/>): which raw request paths
/// need an <c>X-Api-Key</c> while the scheme is configured.
/// </summary>
[TestFixture]
public class ApiRoutePolicyRequiresApiKeyTests
{
    [TestCase("/api/health", false)]
    [TestCase("/api/health/detailed", false)]
    [TestCase("/api/v1.0/health", false)]
    [TestCase("/api/v1.0/health/detailed", false)]
    [TestCase("/api/diagnostics", true)]
    [TestCase("/api/v1.0/diagnostics", true)]
    [TestCase("/api/version", false)]
    [TestCase("/api/v1.0/version", false)]
    [TestCase("/api/time", false)]
    [TestCase("/api/v1.0/time", false)]
    [TestCase("/api/ping", false)]
    [TestCase("/api/v1.0/ping", false)]
    [TestCase("/api/tools/work-order-statuses", false)]
    [TestCase("/api/v1.0/tools/due-date-check", false)]
    [TestCase("/api/work-orders/bulk-import", true)]
    [TestCase("/api/status/environment", true)]
    [TestCase("/api/v1.0/status/environment", true)]
    [TestCase("/api", true)]
    [TestCase("/api/", true)]
    [TestCase("/API/v1.0/diagnostics", true)]
    [TestCase("/apifoo", false)]
    [TestCase("/", false)]
    [TestCase("api/time", false)]
    [TestCase("api/diagnostics", false)]
    public void RequiresApiKey_ReturnsExpected_When_PathAndOptions(string path, bool expectedValidation)
    {
        var options = ApiKeyAuthenticationOptionsMother.Enabled();
        ApiRoutePolicy.RequiresApiKey(path, options).ShouldBe(expectedValidation);
    }

    [Test]
    public void RequiresApiKey_ReturnsFalse_When_Disabled()
    {
        var options = ApiKeyAuthenticationOptionsMother.Disabled();
        ApiRoutePolicy.RequiresApiKey("/api/health", options).ShouldBeFalse();
    }

    [Test]
    public void RequiresApiKey_ReturnsFalse_When_KeyEmpty()
    {
        var options = ApiKeyAuthenticationOptionsMother.EnabledWithBlankKey();
        ApiRoutePolicy.RequiresApiKey("/api/health", options).ShouldBeFalse();
    }

    [Test]
    public void RequiresApiKey_ReturnsFalse_When_PathEmpty()
    {
        var options = ApiKeyAuthenticationOptionsMother.Enabled();
        ApiRoutePolicy.RequiresApiKey("", options).ShouldBeFalse();
    }

    [Test]
    public void RequiresApiKey_ReturnsFalse_When_PathNotApi()
    {
        var options = ApiKeyAuthenticationOptionsMother.Enabled();
        ApiRoutePolicy.RequiresApiKey("/mcp", options).ShouldBeFalse();
    }

    [Test]
    public void RequiresApiKey_ReturnsFalse_When_PathNull()
    {
        var options = ApiKeyAuthenticationOptionsMother.Enabled();
        Should.NotThrow(() => ApiRoutePolicy.RequiresApiKey(null, options)).ShouldBeFalse();
    }

    [Test]
    public void RequiresApiKey_ReturnsFalse_When_PathUnrooted()
    {
        var options = ApiKeyAuthenticationOptionsMother.Enabled();
        Should.NotThrow(() => ApiRoutePolicy.RequiresApiKey("api/time", options)).ShouldBeFalse();
    }
}
