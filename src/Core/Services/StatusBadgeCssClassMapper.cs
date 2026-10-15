using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Maps work order status to CSS badge classes for consistent styling across pages.
/// </summary>
public static class StatusBadgeCssClassMapper
{
    /// <summary>
    /// Returns the CSS class for the given work order status badge.
    /// </summary>
    public static string CssClass(WorkOrderStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        return status switch
        {
            _ when status == WorkOrderStatus.Draft => "status-badge--draft",
            _ when status == WorkOrderStatus.Assigned => "status-badge--assigned",
            _ when status == WorkOrderStatus.InProgress => "status-badge--in-progress",
            _ when status == WorkOrderStatus.Complete => "status-badge--complete",
            _ when status == WorkOrderStatus.Cancelled => "status-badge--cancelled",
            _ => string.Empty
        };
    }
}
