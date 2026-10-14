using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>What a status pill shows: its text, its CSS class and optional screen-reader-only text.</summary>
public sealed record StatusPill(string Text, string CssClass, string? HiddenText);

/// <summary>
/// Presents a work order's status as a pill. An open work order past its due date shows "Overdue"
/// (a display state, not a stored status) with the stored status as visually hidden text, so colour
/// is never the only signal and screen readers still hear the real status.
/// </summary>
public static class StatusPillPresenter
{
    private const string Base = "status-badge";

    private static readonly Dictionary<string, string> Modifiers = new()
    {
        [WorkOrderStatus.Draft.Key] = "draft",
        [WorkOrderStatus.Assigned.Key] = "assigned",
        [WorkOrderStatus.InProgress.Key] = "in-progress",
        [WorkOrderStatus.Complete.Key] = "complete",
        [WorkOrderStatus.Cancelled.Key] = "cancelled"
    };

    /// <summary>The pill for <paramref name="workOrder"/>; <paramref name="isOverdue"/> comes from the due-date urgency.</summary>
    public static StatusPill Present(WorkOrder workOrder, bool isOverdue) =>
        isOverdue
            ? new StatusPill("Overdue", $"{Base} {Base}--overdue", $", status {workOrder.Status.FriendlyName}")
            : new StatusPill(workOrder.Status.FriendlyName, $"{Base} {Base}--{Modifier(workOrder.Status)}", null);

    private static string Modifier(WorkOrderStatus status) =>
        Modifiers.TryGetValue(status.Key, out var modifier) ? modifier : "draft";
}
