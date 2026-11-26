using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>
/// Pins how <see cref="ApiPath.Parse"/> splits a raw request path once into IsApi, Version, Leaf and IsPublic.
/// Each test parses the path, then reads the parts it asserts on.
/// </summary>
[TestFixture]
public class ApiPathTests
{
    [TestCase("/api/version", "version", true)]
    [TestCase("/api/time", "time", true)]
    [TestCase("/api/ping", "ping", true)]
    [TestCase("/api/tools/work-order-statuses", "tools/work-order-statuses", true)]
    [TestCase("/api/tools/due-date-check", "tools/due-date-check", true)]
    [TestCase("/api/work-orders/status-counts", "work-orders/status-counts", true)]
    [TestCase("/api/health", "health", true)]
    [TestCase("/api/health/detailed", "health/detailed", true)]
    [TestCase("/api/HEALTH", "HEALTH", true)]
    [TestCase("/api/workorders", "workorders", false)]
    [TestCase("/api/status/environment", "status/environment", false)]
    [TestCase("/api/work-orders/bulk-import", "work-orders/bulk-import", false)]
    [TestCase("/api/", null, false)]
    public void Parse_IsPublic_ReturnsExpected_When_LeafGiven(string path, string? expectedLeaf, bool expectedPublic)
    {
        var apiPath = ApiPath.Parse(path);

        apiPath.Leaf.ShouldBe(expectedLeaf);
        apiPath.IsPublic.ShouldBe(expectedPublic);
    }

    [TestCase("/api/version", true)]
    [TestCase("/api/v1.0/version", true)]
    [TestCase("/api/time", true)]
    [TestCase("/api/v1.0/ping", true)]
    [TestCase("/api/health", true)]
    [TestCase("/api/health/detailed", true)]
    [TestCase("/api/v1.0/health", true)]
    [TestCase("/api/v1.0/health/detailed", true)]
    [TestCase("/api/work-orders/status-counts", true)]
    [TestCase("/api/v1.0/work-orders/status-counts", true)]
    [TestCase("/api/work-orders/bulk-import", false)]
    [TestCase("/api/status/environment", false)]
    [TestCase("/api/v1.0/status/environment", false)]
    [TestCase("/api/workorders", false)]
    [TestCase("/mcp", false)]
    public void Parse_IsPublic_ReturnsExpected_When_PathGiven(string path, bool expectedPublic)
    {
        var apiPath = ApiPath.Parse(path);

        apiPath.IsPublic.ShouldBe(expectedPublic);
    }

    [TestCase("/api/version", "version")]
    [TestCase("/api/v1.0/time", "time")]
    [TestCase("/api/health", "health")]
    [TestCase("/api/health/detailed", "health/detailed")]
    [TestCase("/api/v1.0/health/detailed", "health/detailed")]
    [TestCase("/api/work-orders/status-counts", "work-orders/status-counts")]
    public void Parse_Leaf_ReturnsLeaf_When_PathIsApi(string path, string expectedLeaf)
    {
        var apiPath = ApiPath.Parse(path);

        apiPath.IsApi.ShouldBeTrue();
        apiPath.Leaf.ShouldBe(expectedLeaf);
    }

    [TestCase("/api")]
    [TestCase("/mcp")]
    [TestCase("/other/version")]
    [TestCase("")]
    public void Parse_Leaf_IsNull_When_PathHasNoApiLeaf(string path)
    {
        var apiPath = ApiPath.Parse(path);

        apiPath.Leaf.ShouldBeNull();
        apiPath.IsPublic.ShouldBeFalse();
    }

    [TestCase("/api/health")]
    [TestCase("/api/health/detailed")]
    [TestCase("/api/v1.0/health")]
    [TestCase("/api/v1.0/health/detailed")]
    public void Should_SkipValidation_When_PathIsDetailedHealth(string path)
    {
        var options = ApiKeyAuthenticationOptionsMother.Enabled();

        var apiPath = ApiPath.Parse(path);

        apiPath.IsApi.ShouldBeTrue(path);
        apiPath.Leaf.ShouldNotBeNull(path);
        apiPath.IsPublic.ShouldBeTrue(path);
        ApiRoutePolicy.RequiresApiKey(path, options).ShouldBeFalse(path);
    }

    [TestCase("/api/v1.0/time", "v1.0")]
    [TestCase("/API/v2/health/detailed", "v2")]
    [TestCase("/api/V1.0/time", null)]
    [TestCase("/api/version", null)]
    [TestCase("/api/time", null)]
    [TestCase("/api/work-orders/status-counts", null)]
    [TestCase("/api", null)]
    public void Parse_Version_ReturnsExpected_When_PathGiven(string path, string? expectedVersion)
    {
        var apiPath = ApiPath.Parse(path);

        apiPath.Version.ShouldBe(expectedVersion);
    }

    [TestCase("/api/v1.0/version", "version")]
    [TestCase("/api/version", "version")]
    public void Parse_Leaf_TreatsSecondSegmentAsVersion_When_MoreSegmentsFollow(string path, string expectedLeaf)
    {
        var apiPath = ApiPath.Parse(path);

        apiPath.Leaf.ShouldBe(expectedLeaf);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("api/time")]
    [TestCase("/apifoo/time")]
    [TestCase("/")]
    public void Parse_IsApi_IsFalse_When_PathValueIsNotUnderApi(string? pathValue)
    {
        var apiPath = Should.NotThrow(() => ApiPath.Parse(pathValue));

        apiPath.IsApi.ShouldBeFalse();
        apiPath.Version.ShouldBeNull();
        apiPath.Leaf.ShouldBeNull();
        apiPath.IsPublic.ShouldBeFalse();
    }

    [TestCase("/api")]
    [TestCase("/api/")]
    [TestCase("/API/v1.0/diagnostics")]
    public void Parse_IsApi_IsTrue_When_PathIsUnderApi(string path)
    {
        var apiPath = ApiPath.Parse(path);

        apiPath.IsApi.ShouldBeTrue();
    }
}
