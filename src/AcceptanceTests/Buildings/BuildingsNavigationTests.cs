using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Buildings;

[TestFixture]
public class BuildingsNavigationTests : BuildingsAcceptanceTestBase
{
    [Test]
    public async Task ShouldNavigateToBuildingsPageFromSidebar()
    {
        await LoginAsCurrentUser();

        var buildingsLink = Page.GetByTestId(nameof(NavMenu.Elements.BuildingsAndRooms));
        await Expect(buildingsLink).ToBeVisibleAsync();
        await Expect(buildingsLink).ToContainTextAsync("Buildings & Rooms");

        await Click(nameof(NavMenu.Elements.BuildingsAndRooms));
        await Page.WaitForURLAsync("**/admin/buildings");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buildings & Rooms" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ShouldRenderDarkModeWithoutBreakage()
    {
        await LoginAsCurrentUser();
        await GotoBuildingsPageAsync();

        await Page.EvaluateAsync("() => { document.documentElement.setAttribute('data-theme', 'dark'); }");
        await Task.Delay(500);

        await Expect(Page.Locator("body")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Buildings & Rooms" })).ToBeVisibleAsync();
    }
}
