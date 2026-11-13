using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class PlaywrightOriginTests
{
    [Test]
    public void ShouldExposeOriginAndStorage_WhenConstructed()
    {
        var origin = new PlaywrightOrigin
        {
            Origin = "https://x",
            LocalStorage = [new PlaywrightLocalStorageItem { Name = "a", Value = "b" }]
        };
        origin.Origin.ShouldBe("https://x");
        origin.LocalStorage.Count.ShouldBe(1);
    }
}
