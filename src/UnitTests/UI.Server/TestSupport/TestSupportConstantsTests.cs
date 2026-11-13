using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class TestSupportConstantsTests
{
    [Test]
    public void ShouldExposePathAndHeaderNames()
    {
        TestSupportConstants.PathPrefix.ShouldBe("/api/test-support");
        TestSupportConstants.SecretHeaderName.ShouldBe("X-Test-Support-Secret");
        TestSupportConstants.NeedsRebootHeaderName.ShouldBe("X-Test-Support-NeedsReboot");
    }
}
