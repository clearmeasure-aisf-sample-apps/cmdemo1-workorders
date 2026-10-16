using IndexPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class LandingPageTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task Should_ShowDashboardHeader_WithoutHero_WhenSignedOut()
    {

        var heading = Page.Locator($"h1[data-testid='{nameof(IndexPage.Elements.HomeHeadingTooltip)}']");
        await heading.WaitForAsync();

        var titleColor = await heading.EvaluateAsync<string>("element => window.getComputedStyle(element).color");
        titleColor.ShouldBe("rgb(15, 23, 42)");
        await Expect(Page.Locator(".church-hero, .church-illustration, .lovejoy-quote")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId(nameof(IndexPage.Elements.SignInPrompt))).ToBeVisibleAsync();
    }
}
