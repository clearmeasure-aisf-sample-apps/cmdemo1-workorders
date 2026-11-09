using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>One page of results and the numbers needed to describe it.</summary>
public sealed record SearchResultPage(WorkOrderSearchResultRow[] Rows, int Page, int PageCount, int Total)
{
    /// <summary>Rows per page.</summary>
    public const int PageSize = 25;

    /// <summary>Position of the first row of this page in the whole result (1-based); 0 for an empty page.</summary>
    private int First => Rows.Length == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    /// <summary>Position of the last row of this page in the whole result; 0 for an empty page.</summary>
    private int Last => First + Rows.Length - Math.Sign(Rows.Length);

    /// <summary>"Showing a–b of M work orders".</summary>
    public string RangeText => Total == 0
        ? "Showing 0 of 0 work orders"
        : string.Create(CultureInfo.InvariantCulture, $"Showing {First}–{Last} of {Total} {(Total == 1 ? "work order" : "work orders")}");

    /// <summary>"N of M": rows on this page of the filtered total.</summary>
    public string CountText => string.Create(CultureInfo.InvariantCulture, $"{Rows.Length} of {Total}");

    /// <summary>Page numbers 1..PageCount.</summary>
    public IEnumerable<int> PageNumbers => Enumerable.Range(1, PageCount);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < PageCount;
}

/// <summary>Client-side paging of search results (25 per page).</summary>
public static class WorkOrderSearchPager
{
    /// <summary>The requested page, clamped to the available pages.</summary>
    public static SearchResultPage GetPage(IReadOnlyList<WorkOrderSearchResultRow> rows, int page)
    {
        const int pageSize = SearchResultPage.PageSize;
        var pageCount = Math.Max(1, (int)Math.Ceiling(rows.Count / (double)pageSize));
        var current = Math.Clamp(page, 1, pageCount);
        var slice = rows.Skip((current - 1) * pageSize).Take(pageSize).ToArray();
        return new SearchResultPage(slice, current, pageCount, rows.Count);
    }
}
