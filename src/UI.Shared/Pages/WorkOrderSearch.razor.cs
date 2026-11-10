using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/workorder/search")]
[Authorize]
public partial class WorkOrderSearch : AppComponentBase
{
    private bool _assignedToMe;
    private Employee? _currentUser;
    private WorkOrderSearchResultRow[] _rawResults = [];
    private readonly WorkOrderRowSelection _selection = new();
    private string? _bulkOutcome;
    private SearchPageScript? _script;
    private bool _canCreate;
    private string? _currentUserName;
    private WorkOrderSearchTable? _table;
    private WorkOrderSearchSummary? _summary;
    private WorkOrderSearchUrlState _state = WorkOrderSearchUrlState.Default;
    private WorkOrderSearchUrlState? _lastUrlState;
    private int _searchVersion;
    private bool _showPriorityBadges;

    [Inject] public TimeProvider Clock { get; set; } = TimeProvider.System;
    [Inject] private IFeatureFlags FeatureFlags { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private WorkOrderSearchState SearchState { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IUserSession UserSession { get; set; } = null!;
    [Inject] private WorkOrderBulkActionService BulkActions { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    [SupplyParameterFromQuery] public string? Creator { get; set; }
    [SupplyParameterFromQuery] public string? Assignee { get; set; }
    [SupplyParameterFromQuery] public string? Status { get; set; }
    [SupplyParameterFromQuery] public string? OverdueOnly { get; set; }
    [SupplyParameterFromQuery] public string? DueThisWeekOnly { get; set; }
    [SupplyParameterFromQuery] public string? WaitingOnVendorOnly { get; set; }
    [SupplyParameterFromQuery] public string? Priority { get; set; }
    [SupplyParameterFromQuery] public string? Building { get; set; }
    [SupplyParameterFromQuery] public string? Room { get; set; }
    [SupplyParameterFromQuery] public string? NoBuilding { get; set; }
    [SupplyParameterFromQuery] public string? NoRoom { get; set; }
    [SupplyParameterFromQuery] public string? AssignedToMe { get; set; }
    [SupplyParameterFromQuery] public string? OpenOnly { get; set; }
    [SupplyParameterFromQuery] public string? CompletedWithinDays { get; set; }
    [SupplyParameterFromQuery] public string? Q { get; set; }
    [SupplyParameterFromQuery] public string? Sort { get; set; }
    [SupplyParameterFromQuery] public string? Dir { get; set; }
    [SupplyParameterFromQuery] public string? Page { get; set; }
    [SupplyParameterFromQuery] public string? View { get; set; }

    private List<string> BuildingOptions { get; set; } = [];

    private IReadOnlyList<FilterChip> Chips => WorkOrderFilterChips.Build(_state, EmployeeName);

    private bool HasActiveFilters => _state.HasAnyFilter;

    /// <summary>What "Save current view" saves: the search on screen, from its first page.</summary>
    private string SavedViewQuery => (_state with { Page = 1 }).ToQueryString();

    protected override async Task OnInitializedAsync()
    {
        _showPriorityBadges = await FeatureFlags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges);
        StatusOptions = WorkOrderStatus.GetAllItems().Select(s => new SelectListItem(s.Key, s.FriendlyName)).ToList();
        var employees = await Bus.Send(new EmployeeGetAllQuery());
        UserOptions = employees.Select(e => new SelectListItem(e.UserName, e.DisplayName)).ToList();
        if (await FeatureFlags.IsEnabledAsync(FeatureFlagNames.BuildingManagement))
        {
            BuildingOptions = (await Bus.Send(new WorkOrderBuildingsQuery())).ToList();
        }
        _currentUserName = (await AuthStateProvider.GetAuthenticationStateAsync()).User.Identity?.Name;
        _currentUser = await UserSession.GetCurrentUserAsync();
        _canCreate = _currentUser?.CanCreateWorkOrder() == true;
        await LoadSummaryAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        var incoming = IncomingState();
        if (incoming == _lastUrlState)
        {
            return;
        }

        _lastUrlState = incoming;
        await ApplyStateAsync(WithSessionAssignedToMe(incoming));
    }

    private WorkOrderSearchUrlState IncomingState() => WorkOrderSearchUrlState.FromQuery(new Dictionary<string, string?>
    {
        [nameof(Creator)] = Creator,
        [nameof(Assignee)] = Assignee,
        [nameof(Status)] = Status,
        [nameof(OverdueOnly)] = OverdueOnly,
        [nameof(DueThisWeekOnly)] = DueThisWeekOnly,
        [nameof(WaitingOnVendorOnly)] = WaitingOnVendorOnly,
        [nameof(Priority)] = Priority,
        [nameof(Building)] = Building,
        [nameof(Room)] = Room,
        [nameof(NoBuilding)] = NoBuilding,
        [nameof(NoRoom)] = NoRoom,
        [nameof(AssignedToMe)] = AssignedToMe,
        [nameof(OpenOnly)] = OpenOnly,
        [nameof(CompletedWithinDays)] = CompletedWithinDays,
        [nameof(Q)] = Q,
        [nameof(Sort)] = Sort,
        [nameof(Dir)] = Dir,
        [nameof(Page)] = Page,
        [nameof(View)] = View
    });

    /// <summary>
    /// The session "Assigned to me" preference applies on a bare URL; any filter in the URL wins.
    /// The URL itself is not rewritten (navigation to <c>/workorder/search</c> stays as it is).
    /// </summary>
    private WorkOrderSearchUrlState WithSessionAssignedToMe(WorkOrderSearchUrlState incoming) =>
        !incoming.HasAnyFilter && SearchState.AssignedToMe ? incoming with { AssignedToMe = true } : incoming;

    private async Task ApplyStateAsync(WorkOrderSearchUrlState state)
    {
        _state = state;
        _selection.Clear();
        _table?.CloseRowMenu();
        _assignedToMe = state.AssignedToMe;
        Model.Filters.Show(state, _currentUserName);
        EnsureSelectedBuildingIsListed();
        await SearchWorkOrders();
    }

    /// <summary>
    /// Keeps a building already chosen in the filter visible when the distinct-buildings query is not run.
    /// </summary>
    private void EnsureSelectedBuildingIsListed()
    {
        var building = Model.Filters.Building;
        if (!string.IsNullOrEmpty(building) && !BuildingOptions.Contains(building))
        {
            BuildingOptions.Add(building);
        }
    }

    /// <summary>Applies a state the user chose, searches, and records it in the URL (replace, no history entry).</summary>
    private async Task CommitAsync(WorkOrderSearchUrlState state)
    {
        SearchState.AssignedToMe = state.AssignedToMe;
        await ApplyStateAsync(state);
        if (_state != state)
        {
            return;
        }

        _lastUrlState = state;
        Navigation.NavigateTo(state.ToRelativeUri(), replace: true);
    }

    private Task HandleFiltersChanged() => CommitAsync(Model.Filters.ToState(_state, _assignedToMe));

    private Task HandleAssignedToMeChanged()
    {
        if (!_assignedToMe)
        {
            Model.Filters.Assignee = null;
        }

        return CommitAsync(Model.Filters.ToState(_state, _assignedToMe));
    }

    private Task HandleCardSelected(SummaryCard card) =>
        CommitAsync(WorkOrderSearchUrlState.ForCard(card) with { Sort = _state.Sort, Descending = _state.Descending, View = _state.View });

    private Task HandleChipRemoved(FilterChipKey key) => CommitAsync(WorkOrderFilterChips.Remove(_state, key));

    private Task HandleClearFilters() =>
        CommitAsync(WorkOrderSearchUrlState.Default with { Sort = _state.Sort, Descending = _state.Descending, View = _state.View });

    private async Task LoadSummaryAsync()
    {
        _summary = await Bus.Send(new WorkOrderSearchSummaryQuery(_currentUserName));
    }

    private string EmployeeName(string userName) =>
        UserOptions.FirstOrDefault(u => u.Value == userName)?.Text ?? userName;

    private async Task SearchWorkOrders()
    {
        var version = ++_searchVersion;
        var specification = _state.ToSpecification(
            await FindEmployeeAsync(Model.Filters.Creator), await FindEmployeeAsync(Model.Filters.Assignee));
        var workOrders = await Bus.Send(specification);
        if (version != _searchVersion)
        {
            return;
        }

        _rawResults = workOrders.Select(workOrder => WorkOrderSearchResultRow.For(workOrder, Clock)).ToArray();
        Model.Results = WorkOrderSearchSorter.Sort(_rawResults, _state.Sort, _state.Descending);
        StateHasChanged();
    }

    private async Task<Employee?> FindEmployeeAsync(string? userName) =>
        string.IsNullOrWhiteSpace(userName) ? null : await Bus.Send(new EmployeeByUserNameQuery(userName));

    private SearchResultPage CurrentPage => WorkOrderSearchPager.GetPage(Model.Results, _state.Page);

    /// <summary>The results count pill: the page slice in table view, every result in board view.</summary>
    private string CountPillText =>
        _state.View == SearchView.Board ? WorkOrderBoard.CountText(Model.Results.Length) : CurrentPage.CountText;

    private IReadOnlyList<string> BulkStatusOptions => _currentUser == null
        ? []
        : WorkOrderBulkActionService.ChangeStatusOptions(SelectedRows.Select(r => r.WorkOrder), _currentUser);

    private IEnumerable<WorkOrderSearchResultRow> SelectedRows => Model.Results.Where(r => _selection.IsSelected(r.Number));

    private string SortValue => WorkOrderSearchSorter.OptionValue(_state.Sort, _state.Descending);

    /// <summary>Applies a sort, page or view change without a new search and records it in the URL.</summary>
    private void CommitView(WorkOrderSearchUrlState state)
    {
        _state = state;
        _lastUrlState = state;
        _selection.Clear();
        _table?.CloseRowMenu();
        Model.Results = WorkOrderSearchSorter.Sort(_rawResults, state.Sort, state.Descending);
        Navigation.NavigateTo(state.ToRelativeUri(), replace: true);
    }

    private void HandleSortBy(string column) =>
        CommitView(_state with { Sort = column, Descending = _state.Sort == column && !_state.Descending, Page = 1 });

    private void HandleSortSelected(ChangeEventArgs e)
    {
        var (column, descending) = WorkOrderSearchSorter.ParseOption(e.Value?.ToString());
        CommitView(_state with { Sort = column, Descending = descending, Page = 1 });
    }

    private void HandlePageSelected(int page) => CommitView(_state with { Page = page });

    private string ViewButtonClass(SearchView view) =>
        _state.View == view ? "view-switch-button is-active" : "view-switch-button";

    private string ViewPressed(SearchView view) => _state.View == view ? "true" : "false";

    /// <summary>Switches between the table and the board without a new search; the view is kept in the URL.</summary>
    private void HandleViewSelected(SearchView view)
    {
        if (view != _state.View)
        {
            CommitView(_state with { View = view, Page = 1 });
        }
    }

    private void HandleClearSelection() => _selection.Clear();

    private async Task HandleBulkAssign(string assigneeUserName)
    {
        var outcome = await BulkActions.AssignAsync(_selection.Numbers.ToArray(), assigneeUserName, _currentUser!);
        await AfterBulkActionAsync(outcome);
    }

    private async Task HandleBulkChangeStatus(string verb)
    {
        var outcome = await BulkActions.ChangeStatusAsync(_selection.Numbers.ToArray(), verb, _currentUser!);
        await AfterBulkActionAsync(outcome);
    }

    private async Task AfterBulkActionAsync(BulkActionOutcome outcome)
    {
        _bulkOutcome = outcome.Text;
        _selection.Clear();
        await SearchWorkOrders();
        await LoadSummaryAsync();
    }

    private Task ExportAllAsync() => DownloadCsvAsync(Model.Results);

    private Task ExportSelectedAsync() => DownloadCsvAsync(SelectedRows);

    private Task DownloadCsvAsync(IEnumerable<WorkOrderSearchResultRow> rows) =>
        (_script ??= new SearchPageScript(Js)).DownloadCsvAsync(
            WorkOrderCsvExporter.FileName(ChurchTimeZone.Today(Clock)), WorkOrderCsvExporter.ToCsv(rows));
}
