// ReSharper disable PropertyCanBeMadeInitOnly.Global -- System.Text.Json round-trip serialization (remoting protocol) requires mutable setters
namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Summary figures for the facility's work order status, grouped by various dimensions.
/// </summary>
public class WorkOrderReportsData
{
    /// <summary>Open work orders grouped by status (Draft, Assigned, In Progress).</summary>
    public Dictionary<string, int> CountByStatus { get; set; } = new();

    /// <summary>Open work orders grouped by priority (High, Medium, Low).</summary>
    public Dictionary<string, int> CountByPriority { get; set; } = new();

    /// <summary>Open work orders grouped by building name (nullable buildings excluded).</summary>
    public Dictionary<string, int> CountByBuilding { get; set; } = new();

    /// <summary>Completed work orders grouped by week-ending date (Sunday), last 8 weeks.</summary>
    public List<WeeklyCount> CompletedPerWeek { get; set; } = new();

    /// <summary>Average days from creation to completion for work orders completed in the last 30 days.</summary>
    public double? AverageDaysToClose { get; set; }

    /// <summary>Open work orders currently overdue (due date before today).</summary>
    public int OverdueCount { get; set; }

    /// <summary>Work orders completed in the last 30 days.</summary>
    public int CompletedLast30Days { get; set; }

    public class WeeklyCount
    {
        public DateOnly WeekEndingDate { get; set; }
        public int Count { get; set; }
    }
}
