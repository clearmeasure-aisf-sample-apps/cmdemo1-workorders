using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.Acceptance;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Waits for a Save Draft on the work order form to land on the search page. A save that never navigates used to
/// surface as a bare 90-second "waiting for navigation" timeout with no hint of why (#2048, master 2.4.2777); this
/// reports what the page showed instead (<see cref="SaveDraftOutcome"/>). The wait itself is unchanged.
/// </summary>
public static class SaveDraftWait
{
    /// <summary>Time allowed for the save and the navigation under a parallel run.</summary>
    private const float TimeoutMs = 90_000;

    /// <summary>
    /// Waits until <paramref name="page"/> is on /workorder/search after saving work order
    /// <paramref name="expectedNumber"/>; throws <see cref="TimeoutException"/> with a diagnosis when it never gets there.
    /// </summary>
    public static async Task ForSearchPageAsync(IPage page, string expectedNumber)
    {
        try
        {
            await page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = TimeoutMs });
        }
        catch (TimeoutException timeout)
        {
            var state = await ReadPageStateAsync(page);
            throw new TimeoutException(SaveDraftOutcome.NotNavigatedMessage(state, expectedNumber, TimeoutMs), timeout);
        }
    }

    private static async Task<SaveDraftPageState> ReadPageStateAsync(IPage page)
    {
        var errorUiShown = await page.Locator(SaveDraftOutcome.ErrorUiSelector).IsVisibleAsync();
        var validationMessages = await page.Locator(SaveDraftOutcome.ValidationMessageSelector).AllInnerTextsAsync();
        var numberLocator = page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        var shownNumber = await numberLocator.CountAsync() > 0 ? (await numberLocator.First.InnerTextAsync()).Trim() : null;
        return new SaveDraftPageState(page.Url, errorUiShown, validationMessages.ToList(), shownNumber);
    }
}
