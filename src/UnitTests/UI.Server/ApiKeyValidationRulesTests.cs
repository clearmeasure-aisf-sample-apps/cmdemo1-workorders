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
    [TestCase("/api", true)]
    [TestCase("/api/", true)]
    [TestCase("/API/v1.0/diagnostics", true)]
    [TestCase("/apifoo", false)]
    [TestCase("/", false)]
    public void RequiresValidation_ReturnsExpected_When_PathAndOptions(string path, bool expectedValidation)
    {
        var options = ApiKeyAuthenticationOptionsMother.Enabled();
        ApiKeyValidationRules.RequiresValidation(path, options).ShouldBe(expectedValidation);
    }

    [Test]
    public void RequiresValidation_ReturnsFalse_When_Disabled()
    {
        var options = ApiKeyAuthenticationOptionsMother.Disabled();
        ApiKeyValidationRules.RequiresValidation("/api/health", options).ShouldBeFalse();
    }

    [Test]
    public void RequiresValidation_ReturnsFalse_When_KeyEmpty()
    {
        var options = ApiKeyAuthenticationOptionsMother.EnabledWithBlankKey();
        ApiKeyValidationRules.RequiresValidation("/api/health", options).ShouldBeFalse();
    }

    [Test]
    public void RequiresValidation_ReturnsFalse_When_PathEmpty()
    {
        var options = ApiKeyAuthenticationOptionsMother.Enabled();
        ApiKeyValidationRules.RequiresValidation("", options).ShouldBeFalse();
    }

    [Test]
    public void RequiresValidation_ReturnsFalse_When_PathNotApi()
    {
        var options = ApiKeyAuthenticationOptionsMother.Enabled();
        ApiKeyValidationRules.RequiresValidation("/mcp", options).ShouldBeFalse();
    }
}
