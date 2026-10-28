using ClearMeasure.Bootcamp.UI.Client.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class RemovedTemplatePageTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowNotFound_WhenOpeningRemovedTemplateRoutes()
    {
        await LoginAsCurrentUser();

        await Page.GotoAsync("/counter");
        await ExpectNotFoundPageAsync();

        await Page.GotoAsync("/fetchdata");
        await ExpectNotFoundPageAsync();
    }

    private async Task ExpectNotFoundPageAsync()
    {
        var alert = Page.GetByRole(AriaRole.Alert);
        await alert.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await Expect(alert).ToContainTextAsync("Sorry, there's nothing at this address.");
        await Expect(Page.GetByTestId(nameof(NotFound.Elements.NotFoundIcon))).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(NotFound.Elements.NotFoundHomeLink))).ToBeVisibleAsync();
    }
}
