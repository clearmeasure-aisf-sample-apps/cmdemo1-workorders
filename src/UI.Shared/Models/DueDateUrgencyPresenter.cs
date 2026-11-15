using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>
/// Presents due-date urgency as CSS classes, screen-reader text and list badges.
/// Domain urgency is computed by <c>DueDateUrgencyCalculator</c>; this type owns presentation only.
/// </summary>
public static class DueDateUrgencyPresenter
{
    /// <summary>
    /// CSS class for the Due Date cell only (empty when no urgency color).
    /// </summary>
    public static string CssClass(DueDateUrgency urgency) =>
        urgency switch
        {
            DueDateUrgency.DueToday => "due-date-today",
            DueDateUrgency.Overdue => "due-date-overdue",
            _ => string.Empty
        };

    /// <summary>
    /// Accessible label next to the date when urgency applies.
    /// </summary>
    public static string? ScreenReaderText(DueDateUrgency urgency) =>
        urgency switch
        {
            DueDateUrgency.DueToday => "Due today",
            DueDateUrgency.Overdue => "Overdue",
            _ => null
        };

    /// <summary>
    /// Badge label for the urgency pill shown in the work order list.
    /// Returns null when no badge should be rendered (no due date).
    /// </summary>
    public static string? BadgeText(DueDateUrgency urgency, DateOnly? dueDate) =>
        urgency switch
        {
            DueDateUrgency.Overdue => "Overdue",
            DueDateUrgency.DueToday => "Due Today",
            _ => dueDate.HasValue ? "On Track" : null
        };
}
