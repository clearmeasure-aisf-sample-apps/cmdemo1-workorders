using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

/// <summary>
/// A due-date patch from an MCP tool: omitted (null) leaves the due date unchanged, blank clears it, otherwise a
/// <c>yyyy-MM-dd</c> date.
/// </summary>
internal readonly record struct DueDatePatch(string? Text)
{
    public string? Error =>
        IsInvalid ? $"Invalid due date '{Text}'. Use yyyy-MM-dd." : null;

    private bool IsInvalid => !string.IsNullOrWhiteSpace(Text) && !TryParse(Text, out _);

    public void ApplyTo(WorkOrder workOrder)
    {
        if (Text == null)
        {
            return;
        }

        workOrder.DueDate = TryParse(Text, out var dueDate) ? dueDate : null;
    }

    private static bool TryParse(string text, out DateOnly dueDate) =>
        DateOnly.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dueDate);
}
