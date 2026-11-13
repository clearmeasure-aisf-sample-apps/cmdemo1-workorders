using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class PlaywrightOriginTests
{
    [Test]
    public void ShouldExposeOriginAndStorage_WhenConstructed()
    {
        var origin = new PlaywrightOrigin("https://x", [new PlaywrightLocalStorageItem("a", "b")]);
        origin.Origin.ShouldBe("https://x");
        origin.LocalStorage.Count.ShouldBe(1);
    }
}
