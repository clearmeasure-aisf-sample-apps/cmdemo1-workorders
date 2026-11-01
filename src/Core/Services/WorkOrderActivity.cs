using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// The most recent dated event of a work order (created, assigned or completed), used to list
/// recent activity without an audit table.
/// </summary>
/// <param name="Verb">The event name shown to people, for example "Completed".</param>
/// <param name="WhenUtc">When the event happened, in UTC.</param>
public sealed record WorkOrderActivity(string Verb, DateTime WhenUtc)
{
    /// <summary>
    /// Gets the church calendar date (America/Chicago) of the event. Stored values come back with
    /// <see cref="DateTimeKind.Unspecified"/>, so the value is treated as UTC.
    /// </summary>
    public DateOnly ChurchDate =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(WhenUtc, DateTimeKind.Utc), ChurchTimeZone.Chicago));

    /// <summary>
    /// Returns the latest of the order's completed, assigned and created dates, or null when it has
    /// none. On equal dates Completed wins over Assigned, and Assigned over Created.
    /// </summary>
    public static WorkOrderActivity? Latest(WorkOrder workOrder)
    {
        WorkOrderActivity?[] candidates =
        [
            Candidate("Completed", workOrder.CompletedDate),
            Candidate("Assigned", workOrder.AssignedDate),
            Candidate("Created", workOrder.CreatedDate)
        ];
        return candidates.OfType<WorkOrderActivity>().MaxBy(a => a.WhenUtc);
    }

    private static WorkOrderActivity? Candidate(string verb, DateTime? when) =>
        when == null ? null : new WorkOrderActivity(verb, when.Value);
}
