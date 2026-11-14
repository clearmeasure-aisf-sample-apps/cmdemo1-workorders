using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>
/// App acceptance base: API-backed seeding keyed by <see cref="AcceptanceTestBase.TestTag"/>.
/// </summary>
public abstract class AppAcceptanceTestBase : AcceptanceApiTestBase
{
    /// <summary>
    /// Toggles light/dark via <see cref="MainLayout.Elements.DarkModeToggle"/> and waits for <c>data-theme</c>.
    /// </summary>
    protected async Task SetThemeAsync(string theme)
    {
        var current = await Page.EvaluateAsync<string>("() => document.documentElement.getAttribute('data-theme')");
        if (current == theme)
        {
            return;
        }

        await Click(nameof(MainLayout.Elements.DarkModeToggle));
        await Page.WaitForFunctionAsync("t => document.documentElement.getAttribute('data-theme') === t", theme);
    }
}
