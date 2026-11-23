using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>
/// Pins the route classification used by API rate limiting: which paths are API paths
/// and which of them the sliding-window limiter applies to. Path literals stay literal
/// so the answers do not drift with the route constants.
/// </summary>
[TestFixture]
public class ApiRoutePolicyTests
{
    [TestCase("/api", true)]
    [TestCase("/api/", true)]
    [TestCase("/api/time", true)]
    [TestCase("/api/v1.0/diagnostics", true)]
    [TestCase("/API/V1.0/DIAGNOSTICS", true)]
    [TestCase("/apifoo", false)]
    [TestCase("/mcp", false)]
    [TestCase("/_healthcheck", false)]
    [TestCase("/", false)]
    public void IsApiPath_ReturnsExpected_When_PathGiven(string path, bool expectedIsApiPath)
    {
        ApiRoutePolicy.IsApiPath(new PathString(path)).ShouldBe(expectedIsApiPath);
    }

    [Test]
    public void IsApiPath_ReturnsFalse_When_PathValueIsNotRooted()
    {
        Should.NotThrow(() => ApiRoutePolicy.IsApiPath((string?)null).ShouldBeFalse());
        Should.NotThrow(() => ApiRoutePolicy.IsApiPath(string.Empty).ShouldBeFalse());
        Should.NotThrow(() => ApiRoutePolicy.IsApiPath("api/time").ShouldBeFalse());
    }

    [TestCase("/api", true)]
    [TestCase("/api/time", true)]
    [TestCase("/api/blazor-wasm-single-api", true)]
    [TestCase("/api/v1.0/blazor-wasm-single-api", true)]
    [TestCase("/API/blazor-wasm-single-api", true)]
    [TestCase("/_healthcheck", false)]
    [TestCase("/_build", false)]
    [TestCase("/mcp", false)]
    [TestCase("/", false)]
    public void IsRateLimitedPath_ReturnsExpected_When_PathGiven(string path, bool expectedIsRateLimited)
    {
        ApiRoutePolicy.IsRateLimitedPath(new PathString(path)).ShouldBe(expectedIsRateLimited);
    }
}
