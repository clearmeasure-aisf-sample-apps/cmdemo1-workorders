using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>One column of the Work Order Search board: a stored status, its pill and the cards in that status.</summary>
public sealed record BoardColumn(WorkOrderStatus Status, StatusPill Pill, WorkOrderSearchResultRow[] Cards);

/// <summary>
/// Groups the Work Order Search results into a board with one column per stored status. Every result row
/// becomes one card; rows keep their order inside a column, so the board follows the chosen sort.
/// </summary>
public static class WorkOrderBoard
{
    /// <summary>
    /// One column for each status in <see cref="WorkOrderStatus.GetAllItems"/> order, empty columns included,
    /// each holding the rows of that status in input order.
    /// </summary>
    public static IReadOnlyList<BoardColumn> Build(IReadOnlyList<WorkOrderSearchResultRow> rows) =>
        WorkOrderStatus.GetAllItems()
            .Select(status => new BoardColumn(status, StatusPillPresenter.ForStatus(status),
                rows.Where(row => row.Status == status).ToArray()))
            .ToArray();

    /// <summary>The results count shown in board view, where every result is on screen: "M of M".</summary>
    public static string CountText(int total) => string.Create(CultureInfo.InvariantCulture, $"{total} of {total}");
}
