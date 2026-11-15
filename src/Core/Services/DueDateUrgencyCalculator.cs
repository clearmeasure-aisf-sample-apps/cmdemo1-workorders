using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Computes due-date urgency at read time using America/Chicago calendar days.
/// </summary>
public static class DueDateUrgencyCalculator
{
    /// <summary>
    /// Returns urgency for the given work order relative to Chicago "today".
    /// </summary>
    public static DueDateUrgency Calculate(WorkOrder workOrder, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(workOrder);
        return Calculate(workOrder.DueDate, workOrder.Status, timeProvider);
    }

    /// <summary>
    /// Returns urgency for an editable due date and work-order status relative to Chicago "today".
    /// </summary>
    public static DueDateUrgency Calculate(
        DateOnly? dueDate,
        WorkOrderStatus status,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (dueDate is not { } due || !WorkOrderSpecifications.IsOpen(status))
        {
            return DueDateUrgency.None;
        }

        return due.CompareTo(ChurchTimeZone.Today(timeProvider)) switch
        {
            0 => DueDateUrgency.DueToday,
            < 0 => DueDateUrgency.Overdue,
            _ => DueDateUrgency.None
        };
    }
}
