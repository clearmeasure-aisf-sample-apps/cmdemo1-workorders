using System.Globalization;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UnitTests.Acceptance;

/// <summary>
/// The &lt;option&gt; an acceptance test chooses in a &lt;select&gt;: how to find it, and what to report when it never
/// renders. Defined in the unit-test assembly, which the acceptance tests reference, so it is tested without a browser.
/// </summary>
internal static class DropdownOption
{
    /// <summary>Every option that carries a value, which leaves out the empty placeholder.</summary>
    internal const string RenderedSelector = "option[value]:not([value=''])";

    /// <summary>CSS selector for the option whose value is <paramref name="value"/>.</summary>
    internal static string Selector(string value) =>
        $"option[value='{value.Replace(@"\", @"\\", StringComparison.Ordinal).Replace("'", @"\'", StringComparison.Ordinal)}']";

    /// <summary>
    /// Explains a timeout waiting for <paramref name="value"/> from the number of options the select did render.
    /// </summary>
    internal static string MissingMessage(string selectTestId, string value, int renderedOptionCount, float timeoutMs)
    {
        var waited = string.Create(CultureInfo.InvariantCulture,
            $"Option '{value}' never rendered in select '{selectTestId}' within {timeoutMs:0} ms; it rendered {renderedOptionCount} options.");
        return waited + " " + Cause(renderedOptionCount);
    }

    private static string Cause(int renderedOptionCount)
    {
        if (renderedOptionCount == 0)
        {
            return "The option list never loaded: check the page for an error loading it.";
        }

        if (renderedOptionCount >= EmployeeGetAllQuery.MaxTake)
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"The list is full at EmployeeGetAllQuery.MaxTake ({EmployeeGetAllQuery.MaxTake}), so the value likely sorts past it (#2006); seed it with LoginDirectoryLastName.");
        }

        return "The list loaded without the value: check that it was created before the page loaded the list.";
    }
}
