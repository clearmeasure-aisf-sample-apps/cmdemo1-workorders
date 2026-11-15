using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Services;

[TestFixture]
public class ThemeScriptTests
{
    [Test]
    public async Task ShouldReadThemeFromModule_WhenGetThemeIsCalled()
    {
        await using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ThemeScript.ModulePath);
        module.Setup<string>("getTheme").SetResult("dark");
        var script = new ThemeScript(ctx.JSInterop.JSRuntime);

        var theme = await script.GetThemeAsync();

        theme.ShouldBe("dark");
        module.VerifyInvoke("getTheme");
    }
}
