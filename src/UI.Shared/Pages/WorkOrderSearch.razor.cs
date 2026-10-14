using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/workorder/search")]
[Authorize]
public partial class WorkOrderSearch : AppComponentBase
{
    private static readonly string[] PriorityOptions = Enum.GetNames<WorkOrderPriority>();

    private string? _sortColumn;
    private bool _sortAscending = true;
    private bool _assignedToMe;
    private bool _canCreate;
    private bool _savedViewsOpen;
    private ElementReference _savedViewsButton;
    private string? _currentUserName;
    private WorkOrderSearchSummary? _summary;
    private WorkOrderSearchUrlState _state = WorkOrderSearchUrlState.Default;
    private WorkOrderSearchUrlState? _lastUrlState;

    [Inject] public TimeProvider Clock { get; set; } = TimeProvider.System;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private WorkOrderSearchState SearchState { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IUserSession UserSession { get; set; } = null!;

    [SupplyParameterFromQuery] public string? Creator { get; set; }
    [SupplyParameterFromQuery] public string? Assignee { get; set; }
    [SupplyParameterFromQuery] public string? Status { get; set; }
    [SupplyParameterFromQuery] public string? OverdueOnly { get; set; }
    [SupplyParameterFromQuery] public string? Priority { get; set; }
    [SupplyParameterFromQuery] public string? Building { get; set; }
    [SupplyParameterFromQuery] public string? AssignedToMe { get; set; }
    [SupplyParameterFromQuery] public string? OpenOnly { get; set; }
    [SupplyParameterFromQuery] public string? CompletedWithinDays { get; set; }
    [SupplyParameterFromQuery] public string? Q { get; set; }
    [SupplyParameterFromQuery] public string? Sort { get; set; }
    [SupplyParameterFromQuery] public string? Dir { get; set; }
    [SupplyParameterFromQuery] public string? Page { get; set; }

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
        StatusOptions = WorkOrderStatus.GetAllItems().Select(s => new SelectListItem(s.Key, s.FriendlyName)).ToList();
        var employees = await Bus.Send(new EmployeeGetAllQuery());
        UserOptions = employees.Select(e => new SelectListItem(e.UserName, e.GetFullName())).ToList();
        BuildingOptions = (await Bus.Send(new WorkOrderBuildingsQuery())).ToList();
        _currentUserName = (await AuthStateProvider.GetAuthenticationStateAsync()).User.Identity?.Name;
        _canCreate = (await UserSession.GetCurrentUserAsync())?.CanCreateWorkOrder() == true;
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
        [nameof(Priority)] = Priority,
        [nameof(Building)] = Building,
        [nameof(AssignedToMe)] = AssignedToMe,
        [nameof(OpenOnly)] = OpenOnly,
        [nameof(CompletedWithinDays)] = CompletedWithinDays,
        [nameof(Q)] = Q,
        [nameof(Sort)] = Sort,
        [nameof(Dir)] = Dir,
        [nameof(Page)] = Page
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
        _assignedToMe = state.AssignedToMe;
        Model.Filters.Creator = state.Creator;
        Model.Filters.Assignee = state.AssignedToMe ? _currentUserName : state.Assignee;
        Model.Filters.Status = state.Status;
        Model.Filters.OverdueOnly = state.OverdueOnly;
        Model.Filters.Priority = state.Priority?.ToString();
        Model.Filters.Building = state.Building;
        await SearchWorkOrders();
    }

    /// <summary>Applies a state the user chose, searches, and records it in the URL (replace, no history entry).</summary>
    private async Task CommitAsync(WorkOrderSearchUrlState state)
    {
        SearchState.AssignedToMe = state.AssignedToMe;
        await ApplyStateAsync(state);
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
        OverdueOnly = Model.Filters.OverdueOnly,
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
        CommitAsync(WorkOrderSearchUrlState.ForCard(card) with { Sort = _state.Sort, Descending = _state.Descending });

    private Task HandleChipRemoved(FilterChipKey key) => CommitAsync(WorkOrderFilterChips.Remove(_state, key));

    private Task HandleClearFilters() =>
        CommitAsync(WorkOrderSearchUrlState.Default with { Sort = _state.Sort, Descending = _state.Descending });

    private async Task HandleSavedViewsKeyDown(KeyboardEventArgs e)
    {
        if (e.Key != "Escape" || !_savedViewsOpen)
        {
            return;
        }

        _savedViewsOpen = false;
        await _savedViewsButton.FocusAsync();
    }

    private async Task LoadSummaryAsync()
    {
        _summary = await Bus.Send(new WorkOrderSearchSummaryQuery(_currentUserName));
    }

    private string EmployeeName(string userName) =>
        UserOptions.FirstOrDefault(u => u.Value == userName)?.Text ?? userName;

    private static string SelectClass(string? value) =>
        string.IsNullOrEmpty(value) ? "form-select filter-select" : "form-select filter-select has-value";

    private async Task SearchWorkOrders()
    {
        var workOrders = await Bus.Send(await BuildSpecificationAsync());
        Model.Results = workOrders.Select(MapSearchRow).ToArray();
        ApplySort();
        StateHasChanged();
    }

    private async Task<WorkOrderSpecificationQuery> BuildSpecificationAsync()
    {
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(await FindEmployeeAsync(Model.Filters.Creator));
        specification.MatchAssignee(await FindEmployeeAsync(Model.Filters.Assignee));
        specification.MatchStatus(_state.Status == null ? null : WorkOrderStatus.FromKey(_state.Status));
        specification.MatchOverdueOnly(_state.OverdueOnly);
        specification.MatchPriority(_state.Priority);
        specification.MatchBuilding(_state.Building);
        specification.MatchOpenOnly(_state.OpenOnly);
        specification.MatchCompletedWithinDays(_state.CompletedWithinDays);
        specification.MatchSearchText(_state.Q);
        return specification;
    }

    private async Task<Employee?> FindEmployeeAsync(string? userName) =>
        string.IsNullOrWhiteSpace(userName) ? null : await Bus.Send(new EmployeeByUserNameQuery(userName));

    private void SortBy(string column)
    {
        if (column == _sortColumn)
            _sortAscending = !_sortAscending;
        else
        {
            _sortColumn = column;
            _sortAscending = true;
        }

        ApplySort();
        StateHasChanged();
    }

    private void ApplySort()
    {
        if (_sortColumn == null) return;
        if (_sortColumn == "Status") ApplySortByStatus();
        else if (_sortColumn == "DueDate") ApplySortByDueDate();
        else if (_sortColumn == "Title") ApplySortByTitle();
        else if (_sortColumn == "Room") ApplySortByRoom();
    }

    private void ApplySortByStatus()
    {
        Model.Results = _sortAscending
            ? Model.Results.OrderBy(r => r.Status.FriendlyName).ToArray()
            : Model.Results.OrderByDescending(r => r.Status.FriendlyName).ToArray();
    }

    private void ApplySortByDueDate()
    {
        Model.Results = _sortAscending
            ? Model.Results.OrderBy(r => r.WorkOrder.DueDate.HasValue ? 0 : 1)
                           .ThenBy(r => r.WorkOrder.DueDate).ToArray()
            : Model.Results.OrderBy(r => r.WorkOrder.DueDate.HasValue ? 0 : 1)
                           .ThenByDescending(r => r.WorkOrder.DueDate).ToArray();
    }

    private void ApplySortByTitle()
    {
        Model.Results = _sortAscending
            ? Model.Results.OrderBy(r => r.Title, StringComparer.CurrentCultureIgnoreCase).ToArray()
            : Model.Results.OrderByDescending(r => r.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    private void ApplySortByRoom()
    {
        Model.Results = _sortAscending
            ? Model.Results.OrderBy(r => r.WorkOrder.RoomNumber, StringComparer.CurrentCultureIgnoreCase).ToArray()
            : Model.Results.OrderByDescending(r => r.WorkOrder.RoomNumber, StringComparer.CurrentCultureIgnoreCase).ToArray();
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
            Urgency = urgency,
            StatusBadgeCssClass = StatusBadgeCssClassMapper.CssClass(workOrder.Status)
        };
    }
}
