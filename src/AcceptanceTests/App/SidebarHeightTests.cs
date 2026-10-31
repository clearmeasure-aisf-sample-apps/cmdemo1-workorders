namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>The navy sidebar stretches to the bottom of a tall page in both themes.</summary>
[TestFixture]
public class SidebarHeightTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task ShouldStretchNavyColumnToPageBottomInBothThemes()
    {
        await Page.SetViewportSizeAsync(1280, 800);
        await Page.EvaluateAsync(
            "() => { const host = document.querySelector('.content-wrapper'); const spacer = document.createElement('div'); spacer.style.height = '2500px'; host.appendChild(spacer); }");

        await Page.EvaluateAsync("() => document.documentElement.setAttribute('data-theme', 'light')");
        await AssertRailReachesPage(2500);

        await Page.EvaluateAsync("() => document.documentElement.setAttribute('data-theme', 'dark')");
        await AssertRailReachesPage(2500);

        var background = await Page.Locator("#app-navigation-rail")
            .EvaluateAsync<string>("e => getComputedStyle(e).backgroundImage");
        background.ShouldContain("rgb(7, 12, 22)");
        background.ShouldContain("rgb(11, 18, 32)");
    }

    private async Task AssertRailReachesPage(double minimum)
    {
        var bottom = await Page.EvaluateAsync<double>(
            "() => { const rail = document.getElementById('app-navigation-rail'); return rail.getBoundingClientRect().bottom + window.scrollY; }");
        var scrollHeight = await Page.EvaluateAsync<double>("() => document.documentElement.scrollHeight");
        bottom.ShouldBeGreaterThanOrEqualTo(minimum);
        scrollHeight.ShouldBeGreaterThanOrEqualTo(minimum);
    }
}
