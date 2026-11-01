using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>
/// Text shown for a person in the user menu and assignee cells: initials, full name and roles.
/// </summary>
public static class UserDisplay
{
    /// <summary>
    /// Two-letter initials from first and last name; falls back to the first two letters of
    /// <paramref name="fallbackName"/>, then "?".
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

    private static bool HasFirstAndLastName(Employee? employee) =>
        employee is { FirstName: { } first, LastName: { } last } && !string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(last);

    private static string InitialsFromName(string? name)
    {
        var letters = (name ?? string.Empty).Where(char.IsLetterOrDigit).Take(2).ToArray();
        return letters.Length == 0 ? "?" : new string(letters).ToUpperInvariant();
    }
}
