using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class TestSupportOptionsTests
{
    [Test]
    public void ShouldDefaultDisabled_WhenConstructed()
    {
        var options = new TestSupportOptions();
        options.Enabled.ShouldBeFalse();
        options.Secret.ShouldBeEmpty();
        TestSupportOptions.SectionName.ShouldBe("TestSupport");
    }
}
