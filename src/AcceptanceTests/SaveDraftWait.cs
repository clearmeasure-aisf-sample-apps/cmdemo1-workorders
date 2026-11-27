using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.Acceptance;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Waits for a Save Draft on the work order form to land on the search page. A save that never navigates used to
/// surface as a bare 90-second "waiting for navigation" timeout with no hint of why (#2048, master 2.4.2777); this
/// reports what the page showed instead (<see cref="SaveDraftOutcome"/>), as an NUnit failure so the tests' own
/// [Retry] applies. The wait itself is unchanged.
/// </summary>
public static class SaveDraftWait
{
    /// <summary>Time allowed for the save and the navigation under a parallel run.</summary>
    private const float TimeoutMs = 90_000;

    /// <summary>
    /// Waits until <paramref name="page"/> is on /workorder/search after saving work order
    /// <paramref name="expectedNumber"/>; fails the test with a diagnosis when it never gets there.
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
            // An NUnit failure, not an error: NUnit 4.3 re-runs a [Retry] test only after an assertion failure, so a
            // TimeoutException here skipped the retries the work order tests declare and failed the build on the spot.
            Assert.Fail(SaveDraftOutcome.NotNavigatedMessage(state, expectedNumber, TimeoutMs) + " (" + timeout.Message.Split('\n')[0] + ")");
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
