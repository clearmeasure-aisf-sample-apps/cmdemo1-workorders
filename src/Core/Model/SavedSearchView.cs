// ReSharper disable PropertyCanBeMadeInitOnly.Global -- Qodana P5 (#9440): EF / System.Text.Json set-by-convention requires mutable setters

namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// A Work Order Search view an employee saved under a name: the search query string without the
/// leading <c>?</c>.
/// </summary>
public class SavedSearchView
{
    public const int MaxNameLength = 100;
    public const int MaxQueryLength = 1000;
    public const int MaxViewsPerEmployee = 20;

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Checks a new view against the rules: a name of 1 to 100 characters (trimmed), a query of at most
    /// 1000 characters, a name not already used (case-insensitive) and fewer than 20 existing views.
    /// </summary>
    /// <returns>The error message, or null when the view is valid.</returns>
    public static string? ValidateNew(string? name, string? query, IReadOnlyCollection<string> existingNames)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            return "Enter a name for the view.";
        if (trimmed.Length > MaxNameLength)
            return $"Name must be {MaxNameLength} characters or fewer.";
        if ((query ?? string.Empty).Length > MaxQueryLength)
            return "Current filters are too long to save.";
        if (existingNames.Any(n => string.Equals(n.Trim(), trimmed, StringComparison.OrdinalIgnoreCase)))
            return $"A view named '{trimmed}' already exists.";
        if (existingNames.Count >= MaxViewsPerEmployee)
            return $"You can save up to {MaxViewsPerEmployee} views. Delete one first.";
        return null;
    }
}
