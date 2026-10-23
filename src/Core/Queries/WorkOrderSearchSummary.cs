// ReSharper disable PropertyCanBeMadeInitOnly.Global -- System.Text.Json round-trip serialization (remoting protocol) requires mutable setters
namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Counts shown on the Work Order Search summary cards and the navigation queue badges.
/// "Open" means Draft, Assigned or In Progress.
/// </summary>
public class WorkOrderSearchSummary
{
    /// <summary>Open work orders.</summary>
    public int OpenCount { get; set; }

    /// <summary>Open work orders created in the last 7 days.</summary>
    public int OpenedLast7Days { get; set; }

    /// <summary>Open work orders assigned to the user.</summary>
    public int AssignedToMeCount { get; set; }

    /// <summary>Work orders assigned to the user in any status.</summary>
    public int AssignedToMeTotal { get; set; }

    /// <summary>Open work orders assigned to the user and due today.</summary>
    public int AssignedToMeDueToday { get; set; }

    /// <summary>Open work orders whose due date is before today.</summary>
    public int OverdueCount { get; set; }

    /// <summary>Open work orders whose due date is today through the next six days.</summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global -- System.Text.Json round-trip serialization (remoting protocol) requires a mutable setter
    public int DueThisWeekCount { get; set; }

    /// <summary>Work orders completed in the last 30 days.</summary>
    public int CompletedLast30Days { get; set; }

    /// <summary>Average days from creation to completion of the work orders completed in the last 30 days.</summary>
    public double? AverageDaysToClose { get; set; }

    /// <summary>Work orders created by the user.</summary>
    public int CreatedByMeCount { get; set; }

    /// <summary>Work orders in progress.</summary>
    public int InProgressCount { get; set; }
}
