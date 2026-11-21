using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the login page employee select only.
/// </summary>
public static class LoginDisplayNameFormatter
{
    private static readonly Regex WhitespaceCollapse = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Returns the full name in uppercase with repeated whitespace collapsed to a single space,
    /// so locally stored mixed-case names match mainframe all-caps names in the login drop-down.
    /// </summary>
    public static string FormatForLoginDropdown(string? fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return string.Empty;

        string trimmed = fullName.Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        return WhitespaceCollapse.Replace(trimmed, " ").ToUpperInvariant();
    }
}
