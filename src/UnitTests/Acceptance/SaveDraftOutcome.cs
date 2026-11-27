using System.Globalization;

namespace ClearMeasure.Bootcamp.UnitTests.Acceptance;

/// <summary>
/// What an acceptance test reports when a Save Draft click on the new work order form never reaches the search page.
/// The page stays put for one of four reasons, and each leaves a different mark: the app threw (Blazor's error bar),
/// the form refused the values (validation messages), the form was re-initialised under the test (a different work
/// order number), or the click never submitted the form. Defined in the unit-test assembly, which the acceptance tests
/// reference, so the diagnosis is tested without a browser.
/// </summary>
internal static class SaveDraftOutcome
{
    /// <summary>Blazor WebAssembly's error bar, shown when the app throws an unhandled exception.</summary>
    internal const string ErrorUiSelector = "#blazor-error-ui";

    /// <summary>The messages the EditForm's ValidationSummary and ValidationMessage components render.</summary>
    internal const string ValidationMessageSelector = ".validation-message";

    /// <summary>
    /// Explains why the page never navigated to the search page after Save Draft for work order
    /// <paramref name="expectedNumber"/>, from what the page showed when the wait ran out.
    /// </summary>
    internal static string NotNavigatedMessage(SaveDraftPageState state, string expectedNumber, float timeoutMs)
    {
        var waited = string.Create(CultureInfo.InvariantCulture,
            $"Save Draft of work order {expectedNumber} did not reach /workorder/search within {timeoutMs:0} ms; the page stayed on {state.Url}.");
        return waited + " " + Cause(state, expectedNumber);
    }

    private static string Cause(SaveDraftPageState state, string expectedNumber)
    {
        if (state.ErrorUiShown)
        {
            return "The app threw while saving (Blazor's error bar is shown): look for the exception in the server log.";
        }

        if (state.ValidationMessages.Count > 0)
        {
            return "The form refused the values: " + string.Join(" | ", state.ValidationMessages);
        }

        if (!string.Equals(state.ShownNumber, expectedNumber, StringComparison.Ordinal))
        {
            return $"The form now shows work order {Shown(state.ShownNumber)}: it was loaded again after the test read the number, which clears what the test typed.";
        }

        return "No error and no validation message: the click did not submit the form.";
    }

    private static string Shown(string? number) => string.IsNullOrEmpty(number) ? "(none)" : number;
}

/// <summary>What the work order page showed when a Save Draft wait ran out.</summary>
internal sealed record SaveDraftPageState(
    string Url,
    bool ErrorUiShown,
    IReadOnlyList<string> ValidationMessages,
    string? ShownNumber);
