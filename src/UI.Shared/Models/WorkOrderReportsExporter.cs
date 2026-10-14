using System.Globalization;
using System.Text;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>
/// Builds the CSV export of Reports page data: status/priority/building counts,
/// weekly completions, and overdue count. Uses RFC 4180 quoting and formula-safe escaping.
/// </summary>
public static class WorkOrderReportsExporter
{
    /// <summary>The CSV text for the reports <paramref name="data"/>, lines separated by CRLF.</summary>
    public static string ToCsv(WorkOrderReportsData data)
    {
        var csv = new StringBuilder();

        // Open by Status section
        AppendLine(csv, ["Category", "Status", "Count"]);
        foreach (var status in GetSortedStatuses(data.CountByStatus))
        {
            AppendLine(csv, ["Open by Status", status, data.CountByStatus[status].ToString()]);
        }

        csv.Append("\r\n");

        // Open by Priority section
        AppendLine(csv, ["Category", "Priority", "Count"]);
        foreach (var priority in GetSortedPriorities(data.CountByPriority))
        {
            AppendLine(csv, ["Open by Priority", priority, data.CountByPriority[priority].ToString()]);
        }

        csv.Append("\r\n");

        // Open by Building section
        AppendLine(csv, ["Category", "Building", "Count"]);
        foreach (var building in data.CountByBuilding.OrderByDescending(kvp => kvp.Value).Take(10))
        {
            AppendLine(csv, ["Open by Building", building.Key, building.Value.ToString()]);
        }

        csv.Append("\r\n");

        // Completed per week section
        AppendLine(csv, ["Week Ending", "Completed"]);
        foreach (var week in data.CompletedPerWeek)
        {
            AppendLine(csv, [week.WeekEndingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), week.Count.ToString()]);
        }

        csv.Append("\r\n");

        // Summary metrics
        AppendLine(csv, ["Metric", "Value"]);
        AppendLine(csv, ["Completed (30 days)", data.CompletedLast30Days.ToString()]);
        AppendLine(csv, ["Average Days to Close", data.AverageDaysToClose?.ToString("0.0", CultureInfo.InvariantCulture) ?? "N/A"]);
        AppendLine(csv, ["Overdue Count", data.OverdueCount.ToString()]);

        return csv.ToString();
    }

    private static IEnumerable<string> GetSortedStatuses(Dictionary<string, int> countByStatus)
    {
        var order = new[] { "Draft", "Assigned", "InProgress" };
        return order.Where(s => countByStatus.ContainsKey(s));
    }

    private static IEnumerable<string> GetSortedPriorities(Dictionary<string, int> countByPriority)
    {
        var order = new[] { "High", "Medium", "Low" };
        return order.Where(p => countByPriority.ContainsKey(p));
    }

    private static void AppendLine(StringBuilder csv, IEnumerable<string> cells)
    {
        csv.Append(string.Join(",", cells.Select(WorkOrderCsvExporter.Escape)));
        csv.Append("\r\n");
    }
}
