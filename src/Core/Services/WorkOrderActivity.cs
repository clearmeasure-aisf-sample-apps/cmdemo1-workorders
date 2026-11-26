using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// The most recent dated event of a work order (created, assigned or completed), used to list
/// recent activity without an audit table.
/// </summary>
/// <param name="Kind">The kind of event; <see cref="WorkOrderActivityKindText.DisplayName"/> gives
/// its person-facing text.</param>
/// <param name="WhenUtc">When the event happened, in UTC.</param>
public sealed record WorkOrderActivity(WorkOrderActivityKind Kind, DateTime WhenUtc)
{
    /// <summary>
    /// Gets the church calendar date of the event; see <see cref="ChurchTimeZone.ChurchDate"/>.
    /// </summary>
    public DateOnly ChurchDate => ChurchTimeZone.ChurchDate(WhenUtc);

    /// <summary>
    /// Returns the latest of the order's completed, assigned and created dates, or null when it has
    /// none. On equal dates the higher <see cref="WorkOrderActivityKind"/> wins: Completed over
    /// Assigned, Assigned over Created.
    /// </summary>
    public static WorkOrderActivity? Latest(WorkOrder workOrder)
    {
        WorkOrderActivity?[] candidates =
        [
            Candidate(WorkOrderActivityKind.Completed, workOrder.CompletedDate),
            Candidate(WorkOrderActivityKind.Assigned, workOrder.AssignedDate),
            Candidate(WorkOrderActivityKind.Created, workOrder.CreatedDate)
        ];
        return candidates
            .OfType<WorkOrderActivity>()
            .OrderByDescending(a => a.WhenUtc)
            .ThenByDescending(a => a.Kind)
            .FirstOrDefault();
    }

    private static WorkOrderActivity? Candidate(WorkOrderActivityKind kind, DateTime? when) =>
        when == null ? null : new WorkOrderActivity(kind, when.Value);
}
