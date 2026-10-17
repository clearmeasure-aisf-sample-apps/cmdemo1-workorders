using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>The summary cards on the Work Order Search page; each applies one filter set.</summary>
public enum SummaryCard
{
    Open,
    AssignedToMe,
    Overdue,
    Completed
}

/// <summary>How the Work Order Search results are shown: a paged table or a board with a column per status.</summary>
public enum SearchView
{
    Table,
    Board
}

/// <summary>
/// The Work Order Search state that lives in the URL query string so a search can be bookmarked,
/// shared and survives a reload. Parsing is forgiving (invalid values fall back to defaults) and
/// <see cref="ToRelativeUri"/> writes only non-default values, so an unfiltered search stays
/// exactly <c>workorder/search</c>.
/// </summary>
public sealed record WorkOrderSearchUrlState
{
    /// <summary>Path of the search page, relative to the app base.</summary>
    private const string SearchPath = "workorder/search";

    /// <summary>Default window of the Completed card, in days.</summary>
    private const int CompletedCardDays = 30;

    public string? Creator { get; init; }
    public string? Assignee { get; init; }
    public string? Status { get; init; }
    public bool OverdueOnly { get; init; }

    /// <summary>When true only In Progress work orders waiting on a vendor match.</summary>
    public bool WaitingOnVendorOnly { get; init; }
    public WorkOrderPriority? Priority { get; init; }
    public string? Building { get; init; }
    public string? Room { get; init; }
    public bool NoBuilding { get; init; }
    public bool NoRoom { get; init; }
    public bool AssignedToMe { get; init; }
    public bool OpenOnly { get; init; }
    public int? CompletedWithinDays { get; init; }
    public string? Q { get; init; }
    public string? Sort { get; init; }
    public bool Descending { get; init; }
    public int Page { get; init; } = 1;

    /// <summary>The results view; <see cref="SearchView.Table"/> is the default and is not written to the URL.</summary>
    public SearchView View { get; init; }

    /// <summary>The state with no filters, no sort and the first page.</summary>
    public static WorkOrderSearchUrlState Default { get; } = new();

    /// <summary>True when any filter (not sort or page) is set.</summary>
    public bool HasAnyFilter => HasPeopleFilter || HasAttributeFilter || HasLocationFilter || HasFlagFilter;

    private bool HasPeopleFilter => HasText(Creator) || HasText(Assignee) || AssignedToMe;

    private bool HasAttributeFilter => HasText(Status) || Priority.HasValue || HasText(Q);

    private bool HasLocationFilter => HasText(Building) || HasText(Room) || NoBuilding || NoRoom;

    private bool HasFlagFilter => OverdueOnly || WaitingOnVendorOnly || OpenOnly || CompletedWithinDays.HasValue;

    /// <summary>
    /// Builds the state from query values keyed by parameter name (case-insensitive).
    /// Unknown or invalid values fall back to the defaults.
    /// </summary>
    public static WorkOrderSearchUrlState FromQuery(IReadOnlyDictionary<string, string?> query)
    {
        var values = new Dictionary<string, string?>(query, StringComparer.OrdinalIgnoreCase);
        return new WorkOrderSearchUrlState
        {
            Creator = Text(values, nameof(Creator)),
            Assignee = Text(values, nameof(Assignee)),
            Status = ParseStatus(Text(values, nameof(Status))),
            OverdueOnly = Flag(values, nameof(OverdueOnly)),
            WaitingOnVendorOnly = Flag(values, nameof(WaitingOnVendorOnly)),
            Priority = ParsePriority(Text(values, nameof(Priority))),
            Building = Text(values, nameof(Building)),
            Room = Text(values, nameof(Room)),
            NoBuilding = Flag(values, nameof(NoBuilding)),
            NoRoom = Flag(values, nameof(NoRoom)),
            AssignedToMe = Flag(values, nameof(AssignedToMe)),
            OpenOnly = Flag(values, nameof(OpenOnly)),
            CompletedWithinDays = ParseDays(Text(values, nameof(CompletedWithinDays))),
            Q = Text(values, nameof(Q)),
            Sort = ParseSort(Text(values, nameof(Sort))),
            Descending = string.Equals(Text(values, "Dir"), "desc", StringComparison.OrdinalIgnoreCase),
            Page = ParsePage(Text(values, nameof(Page))),
            View = ParseView(Text(values, nameof(View)))
        };
    }

    /// <summary>
    /// The search page URL for this state, relative to the app base, with only non-default values.
    /// "Assigned to me" replaces the Assignee parameter.
    /// </summary>
    public string ToRelativeUri()
    {
        var parts = FilterParts().Concat(ViewParts()).Where(p => p.Value != null)
            .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}")
            .ToArray();
        return parts.Length == 0 ? SearchPath : $"{SearchPath}?{string.Join("&", parts)}";
    }

    /// <summary>The query string of <see cref="ToRelativeUri"/> without the leading <c>?</c>, or an empty string.</summary>
    public string ToQueryString()
    {
        var uri = ToRelativeUri();
        var index = uri.IndexOf('?');
        return index < 0 ? string.Empty : uri[(index + 1)..];
    }

    /// <summary>The search page URL, relative to the app base, for a stored query string.</summary>
    public static string RelativeUriForQuery(string query) =>
        string.IsNullOrEmpty(query) ? SearchPath : $"{SearchPath}?{query.TrimStart('?')}";

    /// <summary>The filter set a summary card applies; it replaces every other filter.</summary>
    public static WorkOrderSearchUrlState ForCard(SummaryCard card) => card switch
    {
        SummaryCard.AssignedToMe => new WorkOrderSearchUrlState { AssignedToMe = true, OpenOnly = true },
        SummaryCard.Overdue => new WorkOrderSearchUrlState { OverdueOnly = true },
        SummaryCard.Completed => new WorkOrderSearchUrlState
        {
            Status = WorkOrderStatus.Complete.Key,
            CompletedWithinDays = CompletedCardDays
        },
        _ => new WorkOrderSearchUrlState { OpenOnly = true }
    };

    private IEnumerable<KeyValuePair<string, string?>> FilterParts()
    {
        yield return Pair(nameof(Creator), Creator);
        yield return Pair(nameof(Assignee), AssignedToMe ? null : Assignee);
        yield return Pair(nameof(Status), Status);
        yield return Pair(nameof(Priority), Priority?.ToString());
        yield return Pair(nameof(Building), Building);
        yield return Pair(nameof(NoBuilding), HasText(Building) ? null : FlagText(NoBuilding));
        yield return Pair(nameof(Room), Room);
        yield return Pair(nameof(NoRoom), HasText(Room) ? null : FlagText(NoRoom));
        yield return Pair(nameof(AssignedToMe), FlagText(AssignedToMe));
        yield return Pair(nameof(OpenOnly), FlagText(OpenOnly));
        yield return Pair(nameof(OverdueOnly), FlagText(OverdueOnly));
        yield return Pair(nameof(WaitingOnVendorOnly), FlagText(WaitingOnVendorOnly));
        yield return Pair(nameof(CompletedWithinDays), CompletedWithinDays?.ToString(CultureInfo.InvariantCulture));
        yield return Pair(nameof(Q), Q);
    }

    private IEnumerable<KeyValuePair<string, string?>> ViewParts()
    {
        yield return Pair(nameof(Sort), Sort);
        yield return Pair("Dir", HasText(Sort) && Descending ? "desc" : null);
        yield return Pair(nameof(Page), Page > 1 ? Page.ToString(CultureInfo.InvariantCulture) : null);
        yield return Pair(nameof(View), View == SearchView.Board ? nameof(SearchView.Board) : null);
    }

    private static KeyValuePair<string, string?> Pair(string key, string? value) =>
        new(key, HasText(value) ? value : null);

    private static string? FlagText(bool value) => value ? "true" : null;

    private static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value);

    private static string? Text(Dictionary<string, string?> values, string key) =>
        values.TryGetValue(key, out var value) && HasText(value) ? value!.Trim() : null;

    private static bool Flag(Dictionary<string, string?> values, string key) =>
        bool.TryParse(Text(values, key), out var flag) && flag;

    private static string? ParseStatus(string? key) =>
        WorkOrderStatus.GetAllItems().FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase))?.Key;

    private static WorkOrderPriority? ParsePriority(string? value) =>
        Enum.TryParse<WorkOrderPriority>(value, true, out var priority) && Enum.IsDefined(priority) && !int.TryParse(value, out _)
            ? priority
            : null;

    private static SearchView ParseView(string? value) =>
        Enum.TryParse<SearchView>(value, true, out var view) && Enum.IsDefined(view) && !int.TryParse(value, out _)
            ? view
            : SearchView.Table;

    private static int? ParseDays(string? value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var days) && days is >= 1 and <= 3650
            ? days
            : null;

    private static string? ParseSort(string? value) =>
        SortColumns.All.FirstOrDefault(column => string.Equals(column, value, StringComparison.OrdinalIgnoreCase));

    private static int ParsePage(string? value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var page) && page > 1 ? page : 1;
}
