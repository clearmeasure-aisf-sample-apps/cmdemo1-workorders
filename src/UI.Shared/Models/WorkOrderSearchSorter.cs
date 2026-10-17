namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>Sortable columns of the Work Order Search results table (names used in the URL).</summary>
public static class SortColumns
{
    public const string Number = "Number";
    public const string Title = "Title";
    public const string Status = "Status";
    public const string Priority = "Priority";
    public const string Assignee = "Assignee";
    public const string Room = "Room";
    public const string DueDate = "DueDate";

    /// <summary>All sortable columns in table order.</summary>
    public static IReadOnlyList<string> All { get; } = [Number, Title, Status, Priority, Assignee, Room, DueDate];
}

/// <summary>
/// Client-side sorting of search results. No column keeps the handler order (newest first);
/// rows without a due date stay last in both directions.
/// </summary>
public static class WorkOrderSearchSorter
{
    private static readonly StringComparer Text = StringComparer.CurrentCultureIgnoreCase;

    private static readonly Dictionary<string, Func<IEnumerable<WorkOrderSearchResultRow>, bool, IEnumerable<WorkOrderSearchResultRow>>> Sorters =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [SortColumns.Number] = (rows, desc) => By(rows, r => r.Number, desc, Text),
            [SortColumns.Title] = (rows, desc) => By(rows, r => r.Title, desc, Text),
            [SortColumns.Status] = (rows, desc) => By(rows, r => r.Status.FriendlyName, desc, Text),
            [SortColumns.Priority] = (rows, desc) => By(rows, r => (int)r.WorkOrder.Priority, desc, Comparer<int>.Default),
            [SortColumns.Assignee] = (rows, desc) => By(rows, r => r.Assignee?.GetFullName(), desc, Text),
            [SortColumns.Room] = (rows, desc) => By(rows, r => r.WorkOrder.RoomNumber, desc, Text),
            [SortColumns.DueDate] = SortByDueDate
        };

    /// <summary>True when <paramref name="column"/> is a known sortable column.</summary>
    private static bool IsSortable(string? column) => column != null && Sorters.ContainsKey(column);

    /// <summary>Parses a sort control value such as "DueDate:desc"; an empty or unknown column means no sort.</summary>
    public static (string? Column, bool Descending) ParseOption(string? value)
    {
        var parts = (value ?? string.Empty).Split(':');
        return (IsSortable(parts[0]) ? parts[0] : null, parts.Length > 1 && parts[1] == "desc");
    }

    /// <summary>Returns the rows sorted by <paramref name="column"/>; unknown or null keeps the input order.</summary>
    public static WorkOrderSearchResultRow[] Sort(IEnumerable<WorkOrderSearchResultRow> rows, string? column, bool descending) =>
        IsSortable(column) ? Sorters[column!](rows, descending).ToArray() : rows.ToArray();

    private static IEnumerable<WorkOrderSearchResultRow> By<TKey>(IEnumerable<WorkOrderSearchResultRow> rows,
        Func<WorkOrderSearchResultRow, TKey> key, bool descending, IComparer<TKey> comparer) =>
        descending ? rows.OrderByDescending(key, comparer) : rows.OrderBy(key, comparer);

    private static IEnumerable<WorkOrderSearchResultRow> SortByDueDate(IEnumerable<WorkOrderSearchResultRow> rows, bool descending)
    {
        var withDateFirst = rows.OrderBy(r => r.WorkOrder.DueDate.HasValue ? 0 : 1);
        return descending
            ? withDateFirst.ThenByDescending(r => r.WorkOrder.DueDate)
            : withDateFirst.ThenBy(r => r.WorkOrder.DueDate);
    }
}
