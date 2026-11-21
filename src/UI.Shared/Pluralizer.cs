using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>Formats a count together with the form of a unit name that matches it.</summary>
internal static class Pluralizer
{
    /// <summary>
    /// "<paramref name="count"/> <paramref name="singular"/>" when <paramref name="count"/> is exactly one and the
    /// same with a trailing "s" otherwise: "1 work order", "5 work orders".
    /// </summary>
    public static string Count(int count, string singular) =>
        string.Create(CultureInfo.InvariantCulture, $"{count} {singular}{(count == 1 ? "" : "s")}");
}
