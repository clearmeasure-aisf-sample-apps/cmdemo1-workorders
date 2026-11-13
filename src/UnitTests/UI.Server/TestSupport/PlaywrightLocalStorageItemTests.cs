using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class PlaywrightLocalStorageItemTests
{
    [Test]
    public void ShouldExposeNameAndValue_WhenConstructed()
    {
        var item = new PlaywrightLocalStorageItem { Name = "n", Value = "v" };
        item.Name.ShouldBe("n");
        item.Value.ShouldBe("v");
    }
}
