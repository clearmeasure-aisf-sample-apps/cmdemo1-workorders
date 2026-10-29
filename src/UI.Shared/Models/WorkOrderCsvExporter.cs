using System.Globalization;
using System.Text;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>
/// Builds the CSV export of search results: a header row, RFC 4180 quoting, and a leading
/// apostrophe on cells that start with = + - @ so spreadsheets do not run them as formulas.
/// </summary>
public static class WorkOrderCsvExporter
{
    private static readonly string[] Header =
        ["Work Order", "Title", "Description", "Status", "Priority", "Assignee", "Creator", "Building", "Room", "Due Date", "Created", "Completed"];

    /// <summary>The CSV text for <paramref name="rows"/>, lines separated by CRLF.</summary>
    public static string ToCsv(IEnumerable<WorkOrderSearchResultRow> rows)
    {
        var csv = new StringBuilder();
        AppendLine(csv, Header);
        foreach (var row in rows)
        {
            AppendLine(csv, Cells(row));
        }

        return csv.ToString();
    }

    /// <summary>File name for an export made on <paramref name="date"/>.</summary>
    public static string FileName(DateOnly date) =>
        string.Create(CultureInfo.InvariantCulture, $"work-orders-{date:yyyy-MM-dd}.csv");

    private static string?[] Cells(WorkOrderSearchResultRow row) =>
    [
        row.Number,
        row.Title,
        row.WorkOrder.Description,
        row.Status.FriendlyName,
        row.WorkOrder.Priority.ToString(),
        row.Assignee?.DisplayName,
        row.Creator?.DisplayName,
        row.WorkOrder.Building,
        row.WorkOrder.RoomNumber,
        row.WorkOrder.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        row.WorkOrder.CreatedDate?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        row.WorkOrder.CompletedDate?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
    ];

    private static void AppendLine(StringBuilder csv, IEnumerable<string?> cells)
    {
        csv.Append(string.Join(",", cells.Select(Escape)));
        csv.Append("\r\n");
    }

    /// <summary>Quotes a cell when needed and neutralises formula prefixes.</summary>
    public static string Escape(string? value)
    {
        var text = GuardFormula(value ?? string.Empty);
        return text.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{text.Replace("\"", "\"\"")}\"" : text;
    }

    private static string GuardFormula(string text) =>
        text.Length > 0 && "=+-@\t\r".Contains(text[0]) ? "'" + text : text;
}
