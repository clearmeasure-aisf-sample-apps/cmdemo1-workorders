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
        return Parse(reader, cancellationToken);
    }

    private static WorkOrderBulkImportParseResult Parse(TextReader reader, CancellationToken cancellationToken = default)
    {
        var lineNumber = 0;
        string? headerLine = CsvLineReader.ReadLogicalLine(reader, ref lineNumber, cancellationToken);
        if (headerLine == null)
        {
            return WorkOrderBulkImportParseResult.Fail("CSV is empty.");
        }

        var columnIndex = CsvColumnIndex.FromHeader(headerLine);
        var missingColumn = columnIndex.FindMissingRequiredColumn(RequiredHeaders);
        if (missingColumn != null)
        {
            return WorkOrderBulkImportParseResult.Fail(
                $"Missing required column \"{missingColumn}\". Expected header: Title, Description, CreatorUsername, RoomNumber (optional), Building (optional), Priority (optional), DueDate (optional).");
        }

        var rows = ParseDataRows(reader, columnIndex, ref lineNumber, cancellationToken);
        return WorkOrderBulkImportParseResult.Ok(rows);
    }

    private static List<WorkOrderBulkImportRow> ParseDataRows(
        TextReader reader,
        CsvColumnIndex columnIndex,
        ref int lineNumber,
        CancellationToken cancellationToken)
    {
        var rows = new List<WorkOrderBulkImportRow>();
        while (CsvLineReader.ReadLogicalLine(reader, ref lineNumber, cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddNonEmptyRow(rows, columnIndex, line, lineNumber);
        }

        return rows;
    }

    private static void AddNonEmptyRow(
        List<WorkOrderBulkImportRow> rows,
        CsvColumnIndex columnIndex,
        string line,
        int lineNumber)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            rows.Add(columnIndex.ParseRow(line, lineNumber));
        }
    }

    private static string? NullIfWhitespace(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private sealed class CsvColumnIndex
    {
        private readonly Dictionary<string, int> _columns;

        private CsvColumnIndex(Dictionary<string, int> columns) => _columns = columns;

        public static CsvColumnIndex FromHeader(string headerLine)
        {
            var headerValues = CsvLineReader.SplitCsvLine(headerLine);
            var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headerValues.Count; i++)
            {
                AddUniqueHeader(columnIndex, headerValues[i], i);
            }

            return new CsvColumnIndex(columnIndex);
        }

        private static void AddUniqueHeader(Dictionary<string, int> columnIndex, string rawName, int index)
        {
            var name = rawName.Trim();
            if (name.Length > 0)
            {
                columnIndex.TryAdd(name, index);
            }
        }

        public string? FindMissingRequiredColumn(IEnumerable<string> requiredHeaders)
        {
            foreach (var required in requiredHeaders)
            {
                if (!_columns.ContainsKey(required))
                {
                    return required;
                }
            }

            return null;
        }

        public WorkOrderBulkImportRow ParseRow(string line, int lineNumber)
        {
            var cells = CsvLineReader.SplitCsvLine(line);
            var building = OptionalCell(cells, "Building");
            var description = OptionalCell(cells, "Description");
            var priorityText = OptionalCell(cells, "Priority");
            var dueDateText = OptionalCell(cells, "DueDate");
            var priority = ParsePriority(priorityText);
            var dueDate = ParseDueDate(dueDateText);

            return new WorkOrderBulkImportRow(
                lineNumber,
                OptionalCell(cells, "Title"),
                description,
                OptionalCell(cells, "CreatorUsername"),
                OptionalCell(cells, "RoomNumber"),
                building,
                priority,
                dueDate,
                BuildingError(building) ?? DescriptionError(description) ?? PriorityError(priorityText, priority) ?? DueDateError(dueDateText, dueDate));
        }

        private string? OptionalCell(List<string> cells, string name)
        {
            var index = _columns.GetValueOrDefault(name, -1);
            return index >= 0 && index < cells.Count ? NullIfWhitespace(cells[index]) : null;
        }
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

    private static class CsvLineReader
    {
        internal static string? ReadLogicalLine(TextReader reader, ref int lineNumber, CancellationToken cancellationToken)
        {
            var first = reader.ReadLine();
            if (first == null)
            {
                return null;
            }

            lineNumber++;
            var combined = new StringBuilder(first);
            AppendContinuationLines(reader, combined, ref lineNumber, cancellationToken);
            return combined.ToString();
        }

        private static void AppendContinuationLines(
            TextReader reader,
            StringBuilder combined,
            ref int lineNumber,
            CancellationToken cancellationToken)
        {
            while (HasUnclosedQuotes(combined.ToString()))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var next = reader.ReadLine();
                if (next == null)
                {
                    break;
                }

                lineNumber++;
                combined.Append('\n').Append(next);
            }
        }

        private static bool HasUnclosedQuotes(string line) => CountUnescapedQuotes(line) % 2 != 0;

        private static int CountUnescapedQuotes(string s)
        {
            var count = 0;
            var i = 0;
            while (i < s.Length)
            {
                i += CountQuoteAt(s, i, ref count);
            }

            return count;
        }

        private static int CountQuoteAt(string s, int i, ref int count)
        {
            if (s[i] != '"')
            {
                return 1;
            }

            if (IsEscapedQuote(s, i))
            {
                return 2;
            }

            count++;
            return 1;
        }

        internal static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var i = 0;
            while (i < line.Length)
            {
                i = AppendNextCsvToken(line, i, current, result);
            }

            result.Add(current.ToString());
            return result;
        }

        private static int AppendNextCsvToken(string line, int i, StringBuilder current, List<string> result)
        {
            if (line[i] == '"')
            {
                return AppendQuotedField(line, i + 1, current);
            }

            if (line[i] == ',')
            {
                result.Add(current.ToString());
                current.Clear();
                return i + 1;
            }

            current.Append(line[i]);
            return i + 1;
        }

        private static int AppendQuotedField(string line, int startIndex, StringBuilder current)
        {
            var i = startIndex;
            while (i < line.Length)
            {
                var next = AppendQuotedCharacter(line, i, current);
                if (next < 0)
                {
                    return i + 1;
                }

                i = next;
            }

            return i;
        }

        private static int AppendQuotedCharacter(string line, int i, StringBuilder current)
        {
            if (line[i] != '"')
            {
                current.Append(line[i]);
                return i + 1;
            }

            if (IsEscapedQuote(line, i))
            {
                current.Append('"');
                return i + 2;
            }

            return -1;
        }

        private static bool IsEscapedQuote(string s, int index) =>
            s[index] == '"' && index + 1 < s.Length && s[index + 1] == '"';
    }
}
