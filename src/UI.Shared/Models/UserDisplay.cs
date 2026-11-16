using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>
/// Text shown for a person in the user menu and assignee cells: initials, full name, roles and avatar colour.
/// </summary>
public static class UserDisplay
{
    /// <summary>
    /// Two-letter initials from first and last name; falls back to the first two parts of
    /// <paramref name="fallbackName"/> split on <c>.</c>, <c>_</c>, <c>-</c> or space
    /// (e.g., "homer.simpson" → "HS"). If fewer than two parts exist, the first two
    /// letters/digits are used, then "?".
    /// </summary>
    public static string Initials(Employee? employee, string? fallbackName = null) =>
        HasFirstAndLastName(employee)
            ? $"{char.ToUpperInvariant(employee!.FirstName.Trim()[0])}{char.ToUpperInvariant(employee.LastName.Trim()[0])}"
            : InitialsFromName(fallbackName ?? employee?.UserName);

    /// <summary>Role names joined by " · ", sorted; empty when there are none.</summary>
    public static string Roles(Employee? employee) =>
        employee == null
            ? string.Empty
            : string.Join(" · ", employee.Roles.Select(r => r.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Order(StringComparer.OrdinalIgnoreCase));

    /// <summary>One of six avatar colours ("avatar--0" to "avatar--5"), stable for a given user name.</summary>
    public static string AvatarCssClass(string? userName) =>
        "avatar--" + (userName ?? string.Empty).Aggregate(0, (hash, c) => (hash * 31 + c) % 6);

    private static bool HasFirstAndLastName(Employee? employee) =>
        employee is { FirstName: { } first, LastName: { } last } && !string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(last);

    private static string InitialsFromName(string? name)
    {
        if (name == null)
            return "?";

        var parts = name.Split(new[] { '.', '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var first = FirstLetterOrDigit(parts[0]);
            var second = FirstLetterOrDigit(parts[1]);
            if (first != null && second != null)
                return $"{first}{second}";
        }

        var letters = name.Where(char.IsLetterOrDigit).Take(2).ToArray();
        return letters.Length == 0 ? "?" : new string(letters).ToUpperInvariant();
    }

    private static char? FirstLetterOrDigit(string part)
    {
        for (int i = 0; i < part.Length; i++)
        {
            if (char.IsLetterOrDigit(part[i]))
                return char.ToUpperInvariant(part[i]);
        }
        return null;
    }
}
