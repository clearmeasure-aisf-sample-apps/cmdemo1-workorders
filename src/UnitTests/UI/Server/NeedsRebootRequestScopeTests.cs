using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class NeedsRebootRequestScopeTests
{
    [Test]
    public void TryGetRequestScopedFlag_WhenTestSupportDisabled_ReturnsNull()
    {
        var options = new TestSupportOptions(enabled: false, secret: "s");
        var context = new DefaultHttpContext();

        var result = NeedsRebootRequestScope.TryGetRequestScopedFlag(context, options);

        result.ShouldBeNull();
    }

    [Test]
    public void TryGetRequestScopedFlag_WhenEnabledWithoutHeaders_ReturnsFalse()
    {
        var options = new TestSupportOptions(enabled: true, secret: "s");
        var context = new DefaultHttpContext();

        var result = NeedsRebootRequestScope.TryGetRequestScopedFlag(context, options);

        result.ShouldBe(false);
    }

    [Test]
    public void TryGetRequestScopedFlag_WhenEnabledWithAuthorizedTrue_ReturnsTrue()
    {
        var options = new TestSupportOptions(enabled: true, secret: "s");
        var context = new DefaultHttpContext();
        context.Request.Headers[TestSupportConstants.SecretHeaderName] = "s";
        context.Request.Headers[TestSupportConstants.NeedsRebootHeaderName] = "true";

        var result = NeedsRebootRequestScope.TryGetRequestScopedFlag(context, options);

        result.ShouldBe(true);
    }

    [Test]
    public void TryGetRequestScopedFlag_WhenHttpContextNull_ReturnsFalse()
    {
        var options = new TestSupportOptions(enabled: true, secret: "s");

        var result = NeedsRebootRequestScope.TryGetRequestScopedFlag(null, options);

        result.ShouldBe(false);
    }
}
