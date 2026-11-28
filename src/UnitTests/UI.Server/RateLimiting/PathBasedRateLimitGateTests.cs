using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Shouldly;
using HttpEndpoint = Microsoft.AspNetCore.Http.Endpoint;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.RateLimiting;

[TestFixture]
public class PathBasedRateLimitGateTests
{
    private const string LegacySingleApiPath = "/api/blazor-wasm-single-api";
    private const string VersionedSingleApiPath = "/api/v1.0/blazor-wasm-single-api";

    [TestCase(LegacySingleApiPath)]
    [TestCase(VersionedSingleApiPath)]
    [TestCase("/API/Blazor-Wasm-Single-Api")]
    [TestCase("/mcp")]
    [TestCase("/mcp/messages")]
    [TestCase("/MCP")]
    [TestCase("/api/time")]
    public void IsRateLimitedPath_ReturnsTrue_WhenPathIsApiSingleApiOrMcp(string path)
    {
        PathBasedRateLimitGate.IsRateLimitedPath(new PathString(path)).ShouldBeTrue();
    }

    [TestCase("/")]
    [TestCase("/index.html")]
    [TestCase("/_framework/blazor.webassembly.js")]
    [TestCase("/css/app.css")]
    [TestCase("/workorder/search")]
    [TestCase("/_healthcheck")]
    [TestCase("/mcpx")]
    [TestCase("/blazor-wasm-single-api")]
    public void IsRateLimitedPath_ReturnsFalse_WhenPathIsStaticOrBlazorFallback(string path)
    {
        PathBasedRateLimitGate.IsRateLimitedPath(new PathString(path)).ShouldBeFalse();
    }

    [TestCase(LegacySingleApiPath, true)]
    [TestCase(VersionedSingleApiPath, true)]
    [TestCase("/mcp", true)]
    [TestCase("/api/time", false)]
    [TestCase("/index.html", false)]
    public void EnforcesWithoutMetadata_ReturnsExpected_WhenPathGiven(string path, bool expected)
    {
        PathBasedRateLimitGate.EnforcesWithoutMetadata(new PathString(path)).ShouldBe(expected);
    }

    [TestCase(LegacySingleApiPath)]
    [TestCase(VersionedSingleApiPath)]
    [TestCase("/mcp")]
    public void AppliesTo_ReturnsTrue_WhenPathMatchesWithoutRateLimitAttribute(string path)
    {
        var context = new DefaultHttpContext { Request = { Path = path } };

        ApiRateLimitingDecision.AppliesTo(context, new ApiRateLimitingOptions { Enabled = true }).ShouldBeTrue();
    }

    [Test]
    public void AppliesTo_ReturnsTrue_WhenSingleApiEndpointCarriesOtherMetadataOnly()
    {
        var context = new DefaultHttpContext { Request = { Path = VersionedSingleApiPath } };
        context.SetEndpoint(new HttpEndpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "single-api"));

        ApiRateLimitingDecision.AppliesTo(context, new ApiRateLimitingOptions { Enabled = true }).ShouldBeTrue();
    }

    [Test]
    public void AppliesTo_ReturnsFalse_WhenSingleApiPathButLimitingDisabled()
    {
        var context = new DefaultHttpContext { Request = { Path = LegacySingleApiPath } };

        ApiRateLimitingDecision.AppliesTo(context, new ApiRateLimitingOptions { Enabled = false }).ShouldBeFalse();
    }

    [Test]
    public void AppliesTo_ReturnsFalse_WhenStaticPathCarriesTheApiPolicy()
    {
        var context = new DefaultHttpContext { Request = { Path = "/index.html" } };
        var metadata = new EndpointMetadataCollection(new EnableRateLimitingAttribute(ApiRateLimiting.PolicyName));
        context.SetEndpoint(new HttpEndpoint(_ => Task.CompletedTask, metadata, "static"));

        ApiRateLimitingDecision.AppliesTo(context, new ApiRateLimitingOptions { Enabled = true }).ShouldBeFalse();
    }
}
