using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class PlaywrightStorageStateTests
{
    [Test]
    public void ShouldExposeCookiesAndOrigins_WhenConstructed()
    {
        var state = new PlaywrightStorageState([], [new PlaywrightOrigin("https://x", [])]);
        state.Cookies.ShouldBeEmpty();
        state.Origins.Count.ShouldBe(1);
    }
}
