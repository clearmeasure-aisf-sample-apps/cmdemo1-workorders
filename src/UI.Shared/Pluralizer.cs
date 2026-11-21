using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>Formats a count together with the form of a unit name that matches it.</summary>
internal static class Pluralizer
{
    /// <summary>
    /// "<paramref name="n"/> <paramref name="singular"/>" when <paramref name="n"/> is exactly one and the plural
    /// form otherwise: "1 work order", "5 work orders". The plural form is <paramref name="plural"/> when given
    /// ("2 children") and <paramref name="singular"/> with a trailing "s" when it is not.
    /// </summary>
    public static string Count(int n, string singular, string? plural = null) =>
        string.Create(CultureInfo.InvariantCulture, $"{n} {Noun(n, singular, plural)}");

    private static string Noun(int n, string singular, string? plural) =>
        n == 1 ? singular : plural ?? $"{singular}s";
}
