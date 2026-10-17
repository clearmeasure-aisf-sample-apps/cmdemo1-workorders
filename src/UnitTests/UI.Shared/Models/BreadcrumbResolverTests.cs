using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class BreadcrumbResolverTests
{
    [TestCase("", "Dashboard")]
    [TestCase("workorder/search", "Work Orders/Search")]
    [TestCase("workorder/search?Creator=tlovejoy", "Work Orders/Search")]
    [TestCase("workorder/manage?mode=New", "Work Orders/New")]
    [TestCase("workorder/manage/38173BA?mode=Edit", "Work Orders/WO-38173BA")]
    [TestCase("workorder/manage/38173BA?mode=New", "Work Orders/New")]
    [TestCase("settings", "Settings")]
    [TestCase("ai-agent", "AI Assistant")]
    [TestCase("login", "Sign in")]
    [TestCase("fetchdata#top", "Dashboard/Fetchdata")]
    [TestCase("ai-chat/", "Dashboard/Ai chat")]
    public void Resolve_ReturnsTrailForRoute(string relativeUri, string expected)
    {
        string.Join("/", BreadcrumbResolver.Resolve(relativeUri).Select(c => c.Text)).ShouldBe(expected);
    }

    [Test]
    public void Resolve_LastCrumbHasNoLink_EarlierCrumbsLink()
    {
        var trail = BreadcrumbResolver.Resolve("workorder/search");

        trail[0].Href.ShouldBe("workorder/search");
        trail[^1].Href.ShouldBeNull();
    }
}
