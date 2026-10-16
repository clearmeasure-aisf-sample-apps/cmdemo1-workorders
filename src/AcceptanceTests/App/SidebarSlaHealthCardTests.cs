using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class SidebarSlaHealthCardTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldRenderSlaHealthCard_InSidebar_AfterLogin()
    {
        await Page.SetViewportSizeAsync(1280, 800);
        await LoginAsCurrentUser();

        var card = Page.GetByTestId(nameof(SlaHealthCard.Elements.SlaHealthCard));
        await Expect(card).ToBeVisibleAsync();
    }
}
