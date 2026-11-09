namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class InterFontTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task Should_ReportInterAvailable_WhenAppShellLoads()
    {
        bool available = await Page.EvaluateAsync<bool>(
            "async () => { await document.fonts.load('14px Inter'); return document.fonts.check('14px Inter'); }");
        available.ShouldBeTrue();

        bool fontCdn = await Page.EvaluateAsync<bool>(
            "() => [...document.querySelectorAll('link[rel=\"stylesheet\"]')].some(link => /jsdelivr|fontsource|fonts\\.googleapis|fonts\\.gstatic|typekit/i.test(link.href))");
        fontCdn.ShouldBeFalse();

        await Page.EvaluateAsync("() => document.documentElement.setAttribute('data-theme', 'dark')");

        bool darkAvailable = await Page.EvaluateAsync<bool>(
            "async () => { await document.fonts.load('14px Inter'); return document.fonts.check('14px Inter'); }");
        darkAvailable.ShouldBeTrue();
    }
}
