using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class TestSupportChatClientStatusTests
{
    [Test]
    public void Constructor_ShouldRetainUseStubChatClient()
    {
        var status = new TestSupportChatClientStatus(true);

        status.UseStubChatClient.ShouldBeTrue();
    }

    [Test]
    public void Constructor_WhenFalse_ShouldRetainFalse()
    {
        var status = new TestSupportChatClientStatus(false);

        status.UseStubChatClient.ShouldBeFalse();
    }
}
