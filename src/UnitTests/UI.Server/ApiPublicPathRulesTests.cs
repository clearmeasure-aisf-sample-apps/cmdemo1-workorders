using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiPublicPathRulesTests
{
    [TestCase("version", true)]
    [TestCase("time", true)]
    [TestCase("ping", true)]
    [TestCase("tools/work-order-statuses", true)]
    [TestCase("tools/due-date-check", true)]
    [TestCase("work-orders/status-counts", true)]
    [TestCase("health", true)]
    [TestCase("health/detailed", true)]
    [TestCase("HEALTH", true)]
    [TestCase("workorders", false)]
    [TestCase("status/environment", false)]
    [TestCase("work-orders/bulk-import", false)]
    [TestCase("", false)]
    public void IsPublicLeaf_ReturnsExpected(string leaf, bool expectedPublic)
    {
        ApiPublicPathRules.IsPublicLeaf(leaf).ShouldBe(expectedPublic);
    }

    [TestCase("/api/version", "version")]
    [TestCase("/api/v1.0/time", "time")]
    [TestCase("/api/health/detailed", "health/detailed")]
    [TestCase("/api/v1.0/health/detailed", "health/detailed")]
    [TestCase("/api/work-orders/status-counts", "work-orders/status-counts")]
    public void TryGetLeafSegment_ReturnsLeaf(string path, string expectedLeaf)
    {
        ApiPublicPathRules.TryGetLeafSegment(path, out var leaf).ShouldBeTrue();
        leaf.ShouldBe(expectedLeaf);
    }

    [TestCase("/api")]
    [TestCase("/mcp")]
    [TestCase("/other/version")]
    [TestCase("")]
    public void TryGetLeafSegment_ReturnsFalse_WhenPathIsNotApi(string path)
    {
        ApiPublicPathRules.TryGetLeafSegment(path, out var leaf).ShouldBeFalse();
        leaf.ShouldBeEmpty();
    }
}
