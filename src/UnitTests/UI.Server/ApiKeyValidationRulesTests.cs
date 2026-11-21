using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiKeyValidationRulesTests
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
    public void RequiresValidation_ReturnsExpected_When_PathAndOptions(string path, bool expectedValidation)
    {
        var options = new ApiKeyAuthenticationOptions { Enabled = true, ValidationKey = "secret" };
        ApiKeyValidationRules.RequiresValidation(path, options).ShouldBe(expectedValidation);
    }
}
