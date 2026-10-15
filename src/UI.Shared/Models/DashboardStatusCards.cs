using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>One dashboard card: a status, its work order count and how the card looks.</summary>
/// <param name="Status">The work order status the card counts.</param>
/// <param name="Count">How many work orders have the status.</param>
/// <param name="Tone">The card's tone class.</param>
/// <param name="Icon">The card's Bootstrap icon class.</param>
public sealed record DashboardStatusCard(WorkOrderStatus Status, int Count, string Tone, string Icon);

/// <summary>Builds the dashboard's per-status cards from the status counts.</summary>
public static class DashboardStatusCards
{
    private static readonly Dictionary<string, (string Tone, string Icon)> Looks = new()
    {
        [WorkOrderStatus.Draft.Key] = ("summary-card--status-draft", "bi-pencil-square"),
        [WorkOrderStatus.Assigned.Key] = ("summary-card--status-assigned", "bi-person-check"),
        [WorkOrderStatus.InProgress.Key] = ("summary-card--status-in-progress", "bi-hourglass-split"),
        [WorkOrderStatus.Complete.Key] = ("summary-card--status-complete", "bi-check2-circle"),
        [WorkOrderStatus.Cancelled.Key] = ("summary-card--status-cancelled", "bi-x-circle")
    };

    /// <summary>
    /// Returns one card per status in <see cref="WorkOrderStatus.GetAllItems"/> order; a missing key
    /// or null counts give zero.
    /// </summary>
    public static DashboardStatusCard[] Build(IReadOnlyDictionary<string, int>? counts) =>
        WorkOrderStatus.GetAllItems()
            .Select(status => new DashboardStatusCard(status, CountFor(counts, status),
                Looks[status.Key].Tone, Looks[status.Key].Icon))
            .ToArray();

    private static int CountFor(IReadOnlyDictionary<string, int>? counts, WorkOrderStatus status) =>
        counts?.GetValueOrDefault(status.Key) ?? 0;
}
