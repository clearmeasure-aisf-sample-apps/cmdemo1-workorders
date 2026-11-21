using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiKeyAuthenticationMiddlewareTests
{
    [TestCase("/api/health", true)]
    [TestCase("/api/health/detailed", true)]
    [TestCase("/api/v1.0/health", true)]
    [TestCase("/api/v1.0/health/detailed", true)]
    [TestCase("/api/diagnostics", false)]
    [TestCase("/api/v1.0/diagnostics", false)]
    [TestCase("/api/version", true)]
    [TestCase("/api/v1.0/version", true)]
    [TestCase("/api/time", true)]
    [TestCase("/api/v1.0/time", true)]
    [TestCase("/api/ping", true)]
    [TestCase("/api/v1.0/ping", true)]
    [TestCase("/api/tools/work-order-statuses", true)]
    [TestCase("/api/v1.0/tools/due-date-check", true)]
    [TestCase("/api/work-orders/bulk-import", false)]
    [TestCase("/api/status/environment", false)]
    [TestCase("/api/v1.0/status/environment", false)]
    public void ShouldValidate_ReturnsExpected_When_PathAndOptions(string path, bool expectPublicSkip)
    {
        var options = new ApiKeyAuthenticationOptions { Enabled = true, ValidationKey = "secret" };
        var shouldValidate = ApiKeyAuthenticationMiddleware.ShouldValidate(path, options);
        if (expectPublicSkip)
        {
            shouldValidate.ShouldBeFalse();
        }
        else
        {
            shouldValidate.ShouldBeTrue();
        }
    }

    [Test]
    public void ShouldValidate_ReturnsFalse_When_Disabled()
    {
        var options = new ApiKeyAuthenticationOptions { Enabled = false, ValidationKey = "x" };
        ApiKeyValidationRules.RequiresValidation("/api/health", options).ShouldBeFalse();
    }

    [Test]
    public void ShouldValidate_ReturnsFalse_When_KeyEmpty()
    {
        var options = new ApiKeyAuthenticationOptions { Enabled = true, ValidationKey = "" };
        ApiKeyValidationRules.RequiresValidation("/api/health", options).ShouldBeFalse();
    }

    [Test]
    public void ShouldValidate_ReturnsFalse_When_PathNotApi()
    {
        var options = new ApiKeyAuthenticationOptions { Enabled = true, ValidationKey = "x" };
        ApiKeyValidationRules.RequiresValidation("/mcp", options).ShouldBeFalse();
    }

    [Test]
    public void IsAuthorized_ReturnsTrue_WhenKeyMatches()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers["X-API-Key"] = "secret";
        ApiKeyAuthenticationMiddleware.IsAuthorized(request, "secret").ShouldBeTrue();
    }

    [Test]
    public void IsAuthorized_ReturnsFalse_WhenKeyMissing()
    {
        var request = new DefaultHttpContext().Request;
        ApiKeyAuthenticationMiddleware.IsAuthorized(request, "secret").ShouldBeFalse();
    }
}
