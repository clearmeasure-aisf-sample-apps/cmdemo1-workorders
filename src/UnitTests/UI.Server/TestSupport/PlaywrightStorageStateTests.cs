using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class PlaywrightStorageStateTests
{
    [Test]
    public void ShouldExposeCookiesAndOrigins_WhenConstructed()
    {
        var state = new PlaywrightStorageState
        {
            Cookies = [],
            Origins = [new PlaywrightOrigin { Origin = "https://x", LocalStorage = [] }]
        };
        state.Cookies.ShouldBeEmpty();
        state.Origins.Count.ShouldBe(1);
    }
}
