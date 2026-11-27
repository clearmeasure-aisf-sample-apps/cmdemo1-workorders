using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Computes due-date urgency at read time using America/Chicago calendar days.
/// </summary>
public static class DueDateUrgencyCalculator
{
    /// <summary>
    /// Returns urgency for the given work order as of the church calendar's today.
    /// </summary>
    public static DueDateUrgency Calculate(WorkOrder workOrder, ChurchCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(workOrder);
        ArgumentNullException.ThrowIfNull(calendar);
        return Calculate(workOrder.DueDate, workOrder.Status, calendar.Today);
    }

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
        return Calculate(dueDate, status, ChurchTimeZone.Today(timeProvider));
    }

    /// <summary>
    /// Returns urgency for an editable due date and work-order status relative to <paramref name="today"/>.
    /// </summary>
    public static DueDateUrgency Calculate(DateOnly? dueDate, WorkOrderStatus status, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (dueDate is not { } due || !WorkOrderSpecifications.IsOpen(status))
        {
            return DueDateUrgency.None;
        }

        return due.CompareTo(today) switch
        {
            0 => DueDateUrgency.DueToday,
            < 0 => DueDateUrgency.Overdue,
            _ => DueDateUrgency.None
        };
    }
}
