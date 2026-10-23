using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/workorder/search")]
[Authorize]
public partial class WorkOrderSearch : AppComponentBase
{
    private static readonly string[] PriorityOptions = Enum.GetNames<WorkOrderPriority>();

    private bool _assignedToMe;
    private Employee? _currentUser;
    private WorkOrderSearchResultRow[] _rawResults = [];
    private readonly WorkOrderRowSelection _selection = new();
    private string? _openRowMenu;
    private string? _bulkOutcome;
    private ElementReference _selectAllCheckbox;
    private IJSObjectReference? _exportModule;
    private bool _canCreate;
    private bool _savedViewsOpen;
    private ElementReference _savedViewsButton;
    private string? _currentUserName;
    private SavedSearchView[] _userViews = [];
    private bool _saveFormOpen;
    private bool _focusSaveInput;
    private string _newViewName = string.Empty;
    private string? _saveViewError;
    private string? _deleteViewError;
    private ElementReference _saveViewNameInput;
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

    private static readonly (string Name, string Href)[] SavedViews =
    [
        ("Open work orders", WorkOrderSearchUrlState.ForCard(SummaryCard.Open).ToRelativeUri()),
        ("Assigned to me", WorkOrderSearchUrlState.ForCard(SummaryCard.AssignedToMe).ToRelativeUri()),
        ("Overdue", WorkOrderSearchUrlState.ForCard(SummaryCard.Overdue).ToRelativeUri()),
        ("Completed in last 30 days", WorkOrderSearchUrlState.ForCard(SummaryCard.Completed).ToRelativeUri())
    ];

    protected override async Task OnInitializedAsync()
    {
        _showPriorityBadges = await FeatureFlags.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges);
        StatusOptions = WorkOrderStatus.GetAllItems().Select(s => new SelectListItem(s.Key, s.FriendlyName)).ToList();
        var employees = await Bus.Send(new EmployeeGetAllQuery());
        UserOptions = employees.Select(e => new SelectListItem(e.UserName, e.GetFullName())).ToList();
        BuildingOptions = (await Bus.Send(new WorkOrderBuildingsQuery())).ToList();
        _currentUserName = (await AuthStateProvider.GetAuthenticationStateAsync()).User.Identity?.Name;
        await LoadUserViewsAsync();
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
        _openRowMenu = null;
        _assignedToMe = state.AssignedToMe;
        Model.Filters.Creator = state.Creator;
        Model.Filters.Assignee = state.AssignedToMe ? _currentUserName : state.Assignee;
        Model.Filters.Status = state.Status;
        Model.Filters.OverdueOnly = state.OverdueOnly;
        Model.Filters.WaitingOnVendorOnly = state.WaitingOnVendorOnly;
        Model.Filters.Priority = state.Priority?.ToString();
        Model.Filters.Building = state.Building;
        Model.Filters.Room = state.Room;
        await SearchWorkOrders();
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

    private WorkOrderSearchUrlState StateFromFilters() => _state with
    {
        Creator = NullIfBlank(Model.Filters.Creator),
        Assignee = _assignedToMe ? null : NullIfBlank(Model.Filters.Assignee),
        Status = NullIfBlank(Model.Filters.Status),
        Priority = Enum.TryParse<WorkOrderPriority>(Model.Filters.Priority, out var priority) ? priority : null,
        Building = NullIfBlank(Model.Filters.Building),
        Room = NullIfBlank(Model.Filters.Room),
        OverdueOnly = Model.Filters.OverdueOnly,
        WaitingOnVendorOnly = Model.Filters.WaitingOnVendorOnly,
        AssignedToMe = _assignedToMe,
        Page = 1
    };

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private Task HandleFiltersChanged() => CommitAsync(StateFromFilters());

    private Task HandleAssignedToMeChanged()
    {
        if (!_assignedToMe)
        {
            Model.Filters.Assignee = null;
        }

        return CommitAsync(StateFromFilters());
    }

    private Task HandleCardSelected(SummaryCard card) =>
        CommitAsync(WorkOrderSearchUrlState.ForCard(card) with { Sort = _state.Sort, Descending = _state.Descending, View = _state.View });

    private Task HandleChipRemoved(FilterChipKey key) => CommitAsync(WorkOrderFilterChips.Remove(_state, key));

    private Task HandleClearFilters() =>
        CommitAsync(WorkOrderSearchUrlState.Default with { Sort = _state.Sort, Descending = _state.Descending, View = _state.View });

    private async Task HandleSavedViewsKeyDown(KeyboardEventArgs e)
    {
        if (e.Key != "Escape" || (!_savedViewsOpen && !_saveFormOpen))
        {
            return;
        }

        CloseSavedViews();
        await _savedViewsButton.FocusAsync();
    }

    private void ToggleSavedViews()
    {
        if (_savedViewsOpen)
        {
            CloseSavedViews();
            return;
        }

        _savedViewsOpen = true;
    }

    private void CloseSavedViews()
    {
        _savedViewsOpen = false;
        _saveFormOpen = false;
        _saveViewError = null;
        _newViewName = string.Empty;
    }

    private async Task LoadUserViewsAsync()
    {
        if (_currentUserName is null)
        {
            return;
        }

        try
        {
            _userViews = await Bus.Send(new SavedSearchViewsQuery(_currentUserName));
        }
        catch (Exception)
        {
            // Saved views are an optional extra: a failure loading them (for example a database not yet
            // upgraded with the SavedSearchView table) must never break the search page.
            _userViews = [];
        }
    }

    private void OpenSaveForm()
    {
        _saveFormOpen = true;
        _saveViewError = null;
        _newViewName = string.Empty;
        _focusSaveInput = true;
    }

    private async Task CancelSaveForm()
    {
        _saveFormOpen = false;
        _saveViewError = null;
        _newViewName = string.Empty;
        await _savedViewsButton.FocusAsync();
    }

    private async Task HandleSaveView()
    {
        var query = (_state with { Page = 1 }).ToQueryString();
        var error = SavedSearchView.ValidateNew(_newViewName, query, _userViews.Select(v => v.Name).ToArray());
        if (error is not null || _currentUserName is null)
        {
            _saveViewError = error ?? "Sign in to save a view.";
            return;
        }

        try
        {
            var result = await Bus.Send(new SaveSearchViewCommand(_currentUserName, _newViewName.Trim(), query));
            if (!result.Succeeded)
            {
                _saveViewError = result.Error ?? "The view could not be saved.";
                return;
            }
        }
        catch (Exception)
        {
            _saveViewError = "The view could not be saved. Please try again later.";
            return;
        }

        await LoadUserViewsAsync();
        CloseSavedViews();
    }

    private async Task HandleDeleteView(SavedSearchView view)
    {
        if (_currentUserName is null)
        {
            return;
        }

        try
        {
            var result = await Bus.Send(new DeleteSearchViewCommand(_currentUserName, view.Id));
            if (result.Succeeded)
            {
                await LoadUserViewsAsync();
                _deleteViewError = null;
            }
            else
            {
                _deleteViewError = result.Error ?? "The view could not be deleted.";
            }
        }
        catch (Exception)
        {
            _deleteViewError = "The view could not be deleted. Please try again later.";
        }
    }

    private async Task LoadSummaryAsync()
    {
        _summary = await Bus.Send(new WorkOrderSearchSummaryQuery(_currentUserName));
    }

    private string EmployeeName(string userName) =>
        UserOptions.FirstOrDefault(u => u.Value == userName)?.Text ?? userName;

    private static string SelectClass(string? value) =>
        string.IsNullOrEmpty(value) ? "form-select filter-select" : "form-select filter-select has-value";

    private static string RoomInputClass(string? value) =>
        string.IsNullOrEmpty(value) ? "form-control filter-select" : "form-control filter-select has-value";

    private async Task SearchWorkOrders()
    {
        var version = ++_searchVersion;
        var workOrders = await Bus.Send(await BuildSpecificationAsync());
        if (version != _searchVersion)
        {
            return;
        }

        _rawResults = workOrders.Select(MapSearchRow).ToArray();
        Model.Results = WorkOrderSearchSorter.Sort(_rawResults, _state.Sort, _state.Descending);
        StateHasChanged();
    }

    private async Task<WorkOrderSpecificationQuery> BuildSpecificationAsync()
    {
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(await FindEmployeeAsync(Model.Filters.Creator));
        specification.MatchAssignee(await FindEmployeeAsync(Model.Filters.Assignee));
        specification.MatchStatus(_state.Status == null ? null : WorkOrderStatus.FromKey(_state.Status));
        specification.MatchOverdueOnly(_state.OverdueOnly);
        specification.MatchWaitingOnVendorOnly(_state.WaitingOnVendorOnly);
        specification.MatchPriority(_state.Priority);
        specification.MatchBuilding(_state.Building);
        specification.MatchNoBuilding(_state.NoBuilding);
        specification.MatchRoom(_state.Room);
        specification.MatchNoRoom(_state.NoRoom);
        specification.MatchOpenOnly(_state.OpenOnly);
        specification.MatchCompletedWithinDays(_state.CompletedWithinDays);
        specification.MatchSearchText(_state.Q);
        return specification;
    }

    private async Task<Employee?> FindEmployeeAsync(string? userName) =>
        string.IsNullOrWhiteSpace(userName) ? null : await Bus.Send(new EmployeeByUserNameQuery(userName));

    private SearchResultPage CurrentPage => WorkOrderSearchPager.GetPage(Model.Results, _state.Page);

    /// <summary>The results count pill: the page slice in table view, every result in board view.</summary>
    private string CountPillText =>
        _state.View == SearchView.Board ? WorkOrderBoard.CountText(Model.Results.Length) : CurrentPage.CountText;

    private IReadOnlyList<BoardColumn> BoardColumns => WorkOrderBoard.Build(Model.Results);

    private string[] PageNumbers => CurrentPage.Rows.Select(r => r.Number).ToArray();

    private IReadOnlyList<string> BulkStatusOptions => _currentUser == null
        ? []
        : WorkOrderBulkActionService.ChangeStatusOptions(SelectedRows.Select(r => r.WorkOrder), _currentUser);

    private IEnumerable<WorkOrderSearchResultRow> SelectedRows => Model.Results.Where(r => _selection.IsSelected(r.Number));

    private static readonly (string Value, string Text)[] SortOptions = BuildSortOptions();

    private string SortValue => _state.Sort == null ? string.Empty : $"{_state.Sort}:{(_state.Descending ? "desc" : "asc")}";

    private static (string Value, string Text)[] BuildSortOptions()
    {
        var labels = new Dictionary<string, string>
        {
            [SortColumns.Number] = "Work order", [SortColumns.Title] = "Title", [SortColumns.Status] = "Status",
            [SortColumns.Priority] = "Priority", [SortColumns.Assignee] = "Assignee", [SortColumns.Room] = "Room",
            [SortColumns.DueDate] = "Due date"
        };
        var options = new List<(string, string)> { (string.Empty, "Sort: Newest") };
        foreach (var column in SortColumns.All)
        {
            options.Add(($"{column}:asc", $"Sort: {labels[column]} ↑"));
            options.Add(($"{column}:desc", $"Sort: {labels[column]} ↓"));
        }

        return options.ToArray();
    }

    /// <summary>Applies a sort, page or view change without a new search and records it in the URL.</summary>
    private void CommitView(WorkOrderSearchUrlState state)
    {
        _state = state;
        _lastUrlState = state;
        _selection.Clear();
        _openRowMenu = null;
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

    /// <summary>Screen-reader text next to a board card's avatar.</summary>
    private static string AssigneeText(WorkOrderSearchResultRow card) =>
        card.Assignee is null ? "Unassigned" : $"Assigned to {card.Assignee.GetFullName()}";

    /// <summary>Switches between the table and the board without a new search; the view is kept in the URL.</summary>
    private void HandleViewSelected(SearchView view)
    {
        if (view != _state.View)
        {
            CommitView(_state with { View = view, Page = 1 });
        }
    }

    /// <summary>Header button text: the label, plus " ▲" or " ▼" on the sorted column.</summary>
    private string SortLabel(string column, string label) =>
        _state.Sort != column ? label : $"{label} {(_state.Descending ? "▼" : "▲")}";

    private string AriaSort(string column) =>
        _state.Sort != column ? "none" : _state.Descending ? "descending" : "ascending";

    private string RowClass(WorkOrderSearchResultRow row)
    {
        var classes = new List<string>();
        AddClassIf(classes, row.WorkOrder.IsOverdue(), "overdue-row");
        AddClassIf(classes, _selection.IsSelected(row.Number), "row-selected");
        return string.Join(" ", classes);
    }

    private static void AddClassIf(List<string> classes, bool condition, string cssClass)
    {
        if (condition)
        {
            classes.Add(cssClass);
        }
    }

    private void HandleRowToggled(string number) => _selection.Toggle(number);

    private void HandleSelectAllChanged() => _selection.TogglePage(PageNumbers);

    private void HandleClearSelection() => _selection.Clear();

    private void ToggleRowMenu(string number) => _openRowMenu = _openRowMenu == number ? null : number;

    private async Task HandleRowMenuKeyDown(KeyboardEventArgs e)
    {
        if (e.Key != "Escape" || _openRowMenu == null)
        {
            return;
        }

        var trigger = $"row-menu-{_openRowMenu}";
        _openRowMenu = null;
        var module = await ExportModuleAsync();
        await module.InvokeVoidAsync("focusById", trigger);
    }

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

    private async Task DownloadCsvAsync(IEnumerable<WorkOrderSearchResultRow> rows)
    {
        var module = await ExportModuleAsync();
        await module.InvokeVoidAsync("downloadCsv",
            WorkOrderCsvExporter.FileName(ChurchTimeZone.Today(Clock)), WorkOrderCsvExporter.ToCsv(rows));
    }

    private async Task<IJSObjectReference> ExportModuleAsync() =>
        _exportModule ??= await Js.InvokeAsync<IJSObjectReference>("import",
            "./_content/ClearMeasure.Bootcamp.UI.Shared/js/searchExport.js");

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await FocusSaveInputIfRequestedAsync();

        if (Model.Results.Length == 0 || _state.View == SearchView.Board)
        {
            return;
        }

        try
        {
            var module = await ExportModuleAsync();
            await module.InvokeVoidAsync("setIndeterminate", _selectAllCheckbox, _selection.IsIndeterminate(PageNumbers));
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private async Task FocusSaveInputIfRequestedAsync()
    {
        if (!_focusSaveInput || !_saveFormOpen)
        {
            return;
        }

        _focusSaveInput = false;
        await _saveViewNameInput.FocusAsync();
    }

    private WorkOrderSearchResultRow MapSearchRow(WorkOrder workOrder)
    {
        var urgency = DueDateUrgencyCalculator.Calculate(workOrder, Clock);
        return new WorkOrderSearchResultRow
        {
            WorkOrder = workOrder,
            DueDateDisplay = workOrder.DueDate?.ToString("MMM d, yyyy", CultureInfo.InvariantCulture),
            DueDateCssClass = DueDateUrgencyCalculator.CssClass(urgency),
            DueDateUrgencyText = DueDateUrgencyCalculator.ScreenReaderText(urgency),
            Urgency = urgency
        };
    }
}
