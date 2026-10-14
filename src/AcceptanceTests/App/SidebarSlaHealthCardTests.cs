using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class SidebarSlaHealthCardTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldRenderSlaHealthCard_InSidebar_AfterLogin()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var card = Page.GetByTestId(nameof(SlaHealthCard.Elements.SlaHealthCard));
        await Expect(card).ToBeVisibleAsync();
    }
}
