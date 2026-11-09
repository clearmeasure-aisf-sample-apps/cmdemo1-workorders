using System.Globalization;
using System.Text;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Import;

/// <summary>
/// Parses CSV text with an optional header row. Supports quoted fields and comma separators.
/// </summary>
public static class WorkOrderBulkImportCsvParser
{
    private static readonly string[] RequiredHeaders = ["Title", "Description", "CreatorUsername"];

    /// <summary>
    /// Parses the stream as UTF-8 (BOM allowed). Returns rows or an error message.
    /// </summary>
    public static WorkOrderBulkImportParseResult Parse(Stream stream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096, leaveOpen: true);
        var records = new CsvLogicalLineReader(reader);
        if (records.ReadRecord(cancellationToken) is not { } headerRecord)
        {
            return WorkOrderBulkImportParseResult.Fail("CSV is empty.");
        }

        var header = CsvHeader.Parse(headerRecord);
        if (header.FirstMissing(RequiredHeaders) is { } missingColumn)
        {
            return WorkOrderBulkImportParseResult.Fail(
                $"Missing required column \"{missingColumn}\". Expected header: Title, Description, CreatorUsername, RoomNumber (optional), Building (optional), Priority (optional), DueDate (optional).");
        }

        return WorkOrderBulkImportParseResult.Ok(ParseRows(records, header, cancellationToken));
    }

    private static List<WorkOrderBulkImportRow> ParseRows(
        CsvLogicalLineReader records,
        CsvHeader header,
        CancellationToken cancellationToken)
    {
        var rows = new List<WorkOrderBulkImportRow>();
        while (records.ReadRecord(cancellationToken) is { } record)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(record))
            {
                rows.Add(ParseRow(header, CsvFields.Split(record), records.LineNumber));
            }
        }

        return rows;
    }

    private static WorkOrderBulkImportRow ParseRow(CsvHeader header, List<string> fields, int lineNumber)
    {
        var building = header.Cell(fields, "Building");
        var description = header.Cell(fields, "Description");
        var priorityText = header.Cell(fields, "Priority");
        var dueDateText = header.Cell(fields, "DueDate");
        var priority = ParsePriority(priorityText);
        var dueDate = ParseDueDate(dueDateText);

        return new WorkOrderBulkImportRow(
            lineNumber,
            header.Cell(fields, "Title"),
            description,
            header.Cell(fields, "CreatorUsername"),
            header.Cell(fields, "RoomNumber"),
            building,
            priority,
            dueDate,
            BuildingError(building) ?? DescriptionError(description) ?? PriorityError(priorityText, priority) ?? DueDateError(dueDateText, dueDate));
    }

    private static WorkOrderPriority? ParsePriority(string? text)
    {
        if (text == null || !char.IsLetter(text[0]))
        {
            return null;
        }

        return Enum.TryParse<WorkOrderPriority>(text, ignoreCase: true, out var value) && Enum.IsDefined(value)
            ? value
            : null;
    }

    private static string? PriorityError(string? text, WorkOrderPriority? parsed) =>
        text != null && parsed == null
            ? $"Priority '{text}' is not valid. Expected one of: {string.Join(", ", Enum.GetNames<WorkOrderPriority>())}."
            : null;

    private static DateOnly? ParseDueDate(string? text) =>
        text != null && DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
            out var value)
            ? value
            : null;

    private static string? DueDateError(string? text, DateOnly? parsed) =>
        text != null && parsed == null ? $"DueDate '{text}' is not a valid date (expected yyyy-MM-dd)." : null;

    private static string? BuildingError(string? building) =>
        building is { Length: > WorkOrder.BuildingMaxLength }
            ? $"Building must be at most {WorkOrder.BuildingMaxLength} characters."
            : null;

    private static string? DescriptionError(string? description) =>
        description is { Length: > WorkOrder.DescriptionMaxLength }
            ? $"Description cannot exceed {WorkOrder.DescriptionMaxLength} characters."
            : null;
}
