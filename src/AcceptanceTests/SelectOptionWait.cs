using ClearMeasure.Bootcamp.UnitTests.Acceptance;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Waits until a &lt;select&gt; renders the option a test is about to choose. Blazor fills dropdowns such as the
/// login User list only after the WebAssembly app boots and the employee list loads, so the select is visible
/// and editable before its options exist. Options in a closed select have no box and are never "visible" to
/// Playwright, so this waits for Attached. A timeout reports how many options did render and the likely cause.
/// </summary>
public static class SelectOptionWait
{
    /// <summary>Time allowed for the app to boot and load the option list under a parallel run.</summary>
    private const float TimeoutMs = 90_000;

    /// <summary>
    /// Waits for the option with <paramref name="value"/> in <paramref name="select"/>, whose test id is
    /// <paramref name="selectTestId"/>; throws <see cref="TimeoutException"/> with a diagnosis when it never renders.
    /// </summary>
    public static async Task ForOptionAsync(ILocator select, string selectTestId, string value)
    {
        try
        {
            await select.Locator(DropdownOption.Selector(value)).WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = TimeoutMs
            });
        }
        catch (TimeoutException timeout)
        {
            var rendered = await select.Locator(DropdownOption.RenderedSelector).CountAsync();
            throw new TimeoutException(
                DropdownOption.MissingMessage(selectTestId, value, rendered, TimeoutMs), timeout);
        }
    }
}
