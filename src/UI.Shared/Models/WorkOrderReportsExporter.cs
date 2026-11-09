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
    private static readonly string[] StatusOrder = ["Draft", "Assigned", "InProgress"];
    private static readonly string[] PriorityOrder = ["High", "Medium", "Low"];

    /// <summary>The CSV text for the reports <paramref name="data"/>, lines separated by CRLF.</summary>
    public static string ToCsv(WorkOrderReportsData data)
    {
        var csv = new StringBuilder();
        AppendOrderedCounts(csv, "Status", "Open by Status", StatusOrder, data.CountByStatus);
        AppendOrderedCounts(csv, "Priority", "Open by Priority", PriorityOrder, data.CountByPriority);
        AppendBuildings(csv, data.CountByBuilding);
        AppendWeeks(csv, data.CompletedPerWeek);
        AppendSummary(csv, data);
        return csv.ToString();
    }

    private static void AppendOrderedCounts(StringBuilder csv, string heading, string category,
        string[] order, Dictionary<string, int> counts)
    {
        AppendLine(csv, ["Category", heading, "Count"]);
        foreach (var key in order)
        {
            if (counts.TryGetValue(key, out var count))
            {
                AppendLine(csv, [category, key, count.ToString(CultureInfo.InvariantCulture)]);
            }
        }

        csv.Append("\r\n");
    }

    private static void AppendBuildings(StringBuilder csv, Dictionary<string, int> countByBuilding)
    {
        AppendLine(csv, ["Category", "Building", "Count"]);
        foreach (var building in countByBuilding.OrderByDescending(kvp => kvp.Value).Take(10))
        {
            AppendLine(csv, ["Open by Building", building.Key, building.Value.ToString(CultureInfo.InvariantCulture)]);
        }

        csv.Append("\r\n");
    }

    private static void AppendWeeks(StringBuilder csv, List<WorkOrderReportsData.WeeklyCount> weeks)
    {
        AppendLine(csv, ["Week Ending", "Completed"]);
        foreach (var week in weeks)
        {
            AppendLine(csv, [week.WeekEndingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                week.Count.ToString(CultureInfo.InvariantCulture)]);
        }

        csv.Append("\r\n");
    }

    private static void AppendSummary(StringBuilder csv, WorkOrderReportsData data)
    {
        AppendLine(csv, ["Metric", "Value"]);
        AppendLine(csv, ["Completed (30 days)", data.CompletedLast30Days.ToString(CultureInfo.InvariantCulture)]);
        AppendLine(csv, ["Average Days to Close", FormatAverage(data.AverageDaysToClose)]);
        AppendLine(csv, ["Overdue Count", data.OverdueCount.ToString(CultureInfo.InvariantCulture)]);
    }

    private static string FormatAverage(double? average) =>
        average?.ToString("0.0", CultureInfo.InvariantCulture) ?? "N/A";

    private static void AppendLine(StringBuilder csv, IEnumerable<string> cells)
    {
        csv.Append(string.Join(",", cells.Select(WorkOrderCsvExporter.Escape)));
        csv.Append("\r\n");
    }
}
