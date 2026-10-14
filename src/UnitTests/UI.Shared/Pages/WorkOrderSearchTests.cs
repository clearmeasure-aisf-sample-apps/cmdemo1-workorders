using Bunit;
using Bunit.TestDoubles;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

public class WorkOrderSearchTests
{
    private const string CurrentUsername = "jpalermo";

    private static BunitContext CreateContext(IBus? bus = null, string loggedInAs = CurrentUsername,
        WorkOrderSearchState? searchState = null, Employee? currentUser = null)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var theBus = bus ?? new StubBus();
        var uiBus = new StubUiBus();
        ctx.Services.AddSingleton(theBus);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(currentUser));
        ctx.Services.AddSingleton<IUiBus>(uiBus);
        ctx.Services.AddSingleton(new WorkOrderBulkActionService(theBus, uiBus));
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton(searchState ?? new WorkOrderSearchState());

        var store = new StubUserSessionStore { Username = loggedInAs };
        var authProvider = new CustomAuthenticationStateProvider(store);
        authProvider.Login(loggedInAs).GetAwaiter().GetResult();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(authProvider);

        return ctx;
    }

    [Test]
    public async Task ShouldRenderSearchHintWithExactText()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var hint = component.Find($"[data-testid='{WorkOrderSearch.Elements.SearchHint}']");
        hint.TextContent.ShouldBe("Find, triage and track maintenance requests across all church facilities.");
    }

    [Test]
    public async Task ShouldRenderTooltipWithExactTitleOnSearchHeading()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var heading = component.Find($"[data-testid='{WorkOrderSearch.Elements.SearchHeadingTooltip}']");
        heading.GetAttribute("title").ShouldBe("Search every work order");
    }

    [Test]
    public async Task ResultsHeader_ZeroResults_ShowsPluralWorkOrders()
    {
        await using var ctx = CreateContext(new StubBus([]));

        var component = ctx.Render<WorkOrderSearch>();

        component.Find($"[data-testid='{WorkOrderSearch.Elements.ResultsCountPill}']").TextContent.ShouldBe("0 of 0");
        component.Find($"[data-testid='{WorkOrderSearch.Elements.ResultsRangeText}']").TextContent.ShouldBe("Showing 0 of 0 work orders · 0 selected");
    }

    [Test]
    public async Task ResultsHeader_OneResult_ShowsSingularWorkOrder()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-001", Title = "Only one", Status = WorkOrderStatus.Draft },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        component.Find($"[data-testid='{WorkOrderSearch.Elements.ResultsCountPill}']").TextContent.ShouldBe("1 of 1");
        component.Find($"[data-testid='{WorkOrderSearch.Elements.ResultsRangeText}']").TextContent.ShouldBe("Showing 1–1 of 1 work order · 0 selected");
    }

    [Test]
    public async Task ResultsHeader_TwoResults_ShowsPluralWorkOrders()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-001", Title = "First", Status = WorkOrderStatus.Draft },
            new WorkOrder { Number = "WO-002", Title = "Second", Status = WorkOrderStatus.Assigned },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        component.Find($"[data-testid='{WorkOrderSearch.Elements.ResultsCountPill}']").TextContent.ShouldBe("2 of 2");
        component.Find($"[data-testid='{WorkOrderSearch.Elements.ResultsRangeText}']").TextContent.ShouldBe("Showing 1–2 of 2 work orders · 0 selected");
    }

    [Test]
    public async Task ShouldLoadDropDownsInitiallyOnLoad()
    {
        await using var ctx = CreateContext();

        // Act
        var component = ctx.Render<WorkOrderSearch>();

        // Assert
        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = component.Find($"#{WorkOrderSearch.Elements.StatusSelect}");

        creatorSelect.ShouldNotBeNull();
        assigneeSelect.ShouldNotBeNull();
        statusSelect.ShouldNotBeNull();

        // Verify user options are loaded (5 employees + "All" option = 6 options)
        var creatorOptions = creatorSelect.QuerySelectorAll("option");
        creatorOptions.Length.ShouldBe(6);
        creatorOptions[0].TextContent.ShouldBe("Anyone");

        var assigneeOptions = assigneeSelect.QuerySelectorAll("option");
        assigneeOptions.Length.ShouldBe(6);
        assigneeOptions[0].TextContent.ShouldBe("Anyone");

        // Verify status options are loaded (4 statuses + "All" option = 5 options)
        var statusOptions = statusSelect.QuerySelectorAll("option");
        statusOptions.Length.ShouldBe(6);
        statusOptions[0].TextContent.ShouldBe("All statuses");
    }

    [Test]
    public async Task ShouldAssociateFilterLabelsWithMatchingSelectIds()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        AssertLabelForMatchesSelectId(component, WorkOrderSearch.Elements.CreatorSelect);
        AssertLabelForMatchesSelectId(component, WorkOrderSearch.Elements.AssigneeSelect);
        AssertLabelForMatchesSelectId(component, WorkOrderSearch.Elements.StatusSelect);
        AssertLabelForMatchesSelectId(component, WorkOrderSearch.Elements.PrioritySelect);
        AssertLabelForMatchesSelectId(component, WorkOrderSearch.Elements.BuildingSelect);
    }

    private static void AssertLabelForMatchesSelectId(IRenderedComponent<WorkOrderSearch> component, WorkOrderSearch.Elements element)
    {
        var id = element.ToString();
        var select = component.Find($"#{id}");
        select.ShouldNotBeNull();
        var label = component.Find($"label[for='{id}']");
        label.ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldLoadWorkOrderTableWithAllFiltersSetToAllOnInitialLoad()
    {
        await using var ctx = CreateContext();

        // Act
        var component = ctx.Render<WorkOrderSearch>();

        // Assert
        var workOrderTable = component.Find(".grid-data");
        workOrderTable.ShouldNotBeNull();

        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderTableWithCreatorFilterOnInitialLoad()
    {
        await using var ctx = CreateContext();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Creator", "somename");
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.Render<WorkOrderSearch>();

        // Assert
        var workOrderTable = component.Find(".grid-data");
        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderTableWithAssigneeFilterOnInitialLoad()
    {
        await using var ctx = CreateContext();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Assignee", "somename");
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.Render<WorkOrderSearch>();

        // Assert
        var workOrderTable = component.Find(".grid-data");
        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderTableWithStatusFilterOnInitialLoad()
    {
        await using var ctx = CreateContext();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Status", WorkOrderStatus.Assigned.Key);
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.Render<WorkOrderSearch>();

        // Assert
        var workOrderTable = component.Find(".grid-data");
        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public async Task AfterInitialLoadSelectingAllThreeOptionsShouldLoadWorkOrders()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        // Act
        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = component.Find($"#{WorkOrderSearch.Elements.StatusSelect}");

        await creatorSelect.ChangeAsync(new() { Value = "jpalermo" });
        await assigneeSelect.ChangeAsync(new() { Value = "hsimpson" });
        await statusSelect.ChangeAsync(new() { Value = WorkOrderStatus.InProgress.Key });

        // Assert
        var workOrderTable = component.Find(".grid-data");
        workOrderTable.ShouldNotBeNull();

        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public async Task ClearFiltersButton_ShouldBeDisabled_WhenAllFiltersAreEmpty()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var clearButton = component.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        clearButton.HasAttribute("disabled").ShouldBeTrue();
    }

    [Test]
    public async Task ClearFiltersButton_ShouldBeEnabled_WhenAtLeastOneFilterIsSet()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        await creatorSelect.ChangeAsync(new() { Value = "jpalermo" });

        var clearButton = component.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        clearButton.HasAttribute("disabled").ShouldBeFalse();
    }

    [Test]
    public async Task SortByStatus_Ascending_SortsResultsByStatusFriendlyName()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.InProgress },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Assigned },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByStatusButton}");
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll($"[data-testid^='{WorkOrderSearch.Elements.StatusCell}'] .status-badge");
        cells[0].TextContent.Trim().ShouldBe(WorkOrderStatus.Assigned.FriendlyName);
        cells[1].TextContent.Trim().ShouldBe(WorkOrderStatus.Draft.FriendlyName);
        cells[2].TextContent.Trim().ShouldBe(WorkOrderStatus.InProgress.FriendlyName);
    }

    [Test]
    public async Task SortByStatus_ClickingAgain_ReversesToDescending()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.InProgress },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Assigned },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByStatusButton}");
        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll($"[data-testid^='{WorkOrderSearch.Elements.StatusCell}'] .status-badge");
        cells[0].TextContent.Trim().ShouldBe(WorkOrderStatus.InProgress.FriendlyName);
        cells[1].TextContent.Trim().ShouldBe(WorkOrderStatus.Draft.FriendlyName);
        cells[2].TextContent.Trim().ShouldBe(WorkOrderStatus.Assigned.FriendlyName);
    }

    [Test]
    public async Task SortByDueDate_Ascending_SortsResultsByDueDate_NullsLast()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.Draft, DueDate = null },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 6, 1) },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 3, 1) },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByDueDateButton}");
        await sortBtn.ClickAsync(new());

        LinkNumbers(component).ShouldBe(["WO-002", "WO-001", "WO-003"]);
    }

    [Test]
    public async Task SortByDueDate_ClickingAgain_ReversesToDescending_NullsLast()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.Draft, DueDate = null },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 6, 1) },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 3, 1) },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByDueDateButton}");
        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        LinkNumbers(component).ShouldBe(["WO-001", "WO-002", "WO-003"]);
    }

    [Test]
    public async Task SortByDifferentColumn_ResetsDirectionToAscending()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.InProgress, DueDate = new DateOnly(2025, 6, 1) },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Assigned, DueDate = new DateOnly(2025, 3, 1) },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft, DueDate = new DateOnly(2025, 9, 1) },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        // Click Status twice → descending
        var statusBtn = component.Find($"#{WorkOrderSearch.Elements.SortByStatusButton}");
        await statusBtn.ClickAsync(new());
        await statusBtn.ClickAsync(new());

        // Now click DueDate → should reset to ascending
        var dueDateBtn = component.Find($"#{WorkOrderSearch.Elements.SortByDueDateButton}");
        await dueDateBtn.ClickAsync(new());

        LinkNumbers(component).ShouldBe(["WO-001", "WO-003", "WO-002"]);
    }

    [Test]
    public async Task SortIndicatorGlyph_ShowsAscendingGlyph_WhenColumnFirstClicked()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByStatusButton}");
        await sortBtn.ClickAsync(new());

        sortBtn.TextContent.ShouldContain("▲");
        sortBtn.TextContent.ShouldNotContain("▼");
    }

    [Test]
    public async Task SortIndicatorGlyph_ShowsDescendingGlyph_WhenSameColumnClickedAgain()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByStatusButton}");
        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        sortBtn.TextContent.ShouldContain("▼");
        sortBtn.TextContent.ShouldNotContain("▲");
    }

    [Test]
    public async Task ClickingClearFiltersButton_ShouldResetAllFiltersToEmpty_AndTriggerSearch()
    {
        var stubBus = new StubBus();
        await using var ctx = CreateContext(stubBus);

        var component = ctx.Render<WorkOrderSearch>();

        // Set all three filters
        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = component.Find($"#{WorkOrderSearch.Elements.StatusSelect}");

        await creatorSelect.ChangeAsync(new() { Value = "jpalermo" });
        await assigneeSelect.ChangeAsync(new() { Value = "hsimpson" });
        await statusSelect.ChangeAsync(new() { Value = WorkOrderStatus.InProgress.Key });

        var sendCountBeforeClear = stubBus.SendCallCount;

        // Click clear
        var clearButton = component.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        await clearButton.ClickAsync(new());

        // Assert selects reset
        component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}").GetAttribute("value").ShouldBeNullOrEmpty();
        component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}").GetAttribute("value").ShouldBeNullOrEmpty();
        component.Find($"#{WorkOrderSearch.Elements.StatusSelect}").GetAttribute("value").ShouldBeNullOrEmpty();

        // Assert search was re-run (at least one more Send call after clearing)
        stubBus.SendCallCount.ShouldBeGreaterThan(sendCountBeforeClear);
    }

    [Test]
    public async Task SortByTitle_Ascending_SortsResultsByTitle()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.Draft },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Draft },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByTitleButton}");
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll($"[data-testid^='{WorkOrderSearch.Elements.TitleCell}']");
        cells[0].TextContent.Trim().ShouldBe("A");
        cells[1].TextContent.Trim().ShouldBe("B");
        cells[2].TextContent.Trim().ShouldBe("C");
    }

    [Test]
    public async Task SortByTitle_ClickingAgain_ReversesToDescending()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.Draft },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Draft },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByTitleButton}");
        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll($"[data-testid^='{WorkOrderSearch.Elements.TitleCell}']");
        cells[0].TextContent.Trim().ShouldBe("C");
        cells[1].TextContent.Trim().ShouldBe("B");
        cells[2].TextContent.Trim().ShouldBe("A");
    }

    [Test]
    public async Task SortByRoom_Ascending_SortsResultsByRoomNumber()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.Draft, RoomNumber = "C" },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Draft, RoomNumber = "A" },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft, RoomNumber = "B" },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByRoomButton}");
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll($"[data-testid^='{WorkOrderSearch.Elements.RoomCell}']");
        cells[0].TextContent.Trim().ShouldBe("A");
        cells[1].TextContent.Trim().ShouldBe("B");
        cells[2].TextContent.Trim().ShouldBe("C");
    }

    [Test]
    public async Task SortByRoom_ClickingAgain_ReversesToDescending()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-003", Title = "C", Status = WorkOrderStatus.Draft, RoomNumber = "C" },
            new WorkOrder { Number = "WO-001", Title = "A", Status = WorkOrderStatus.Draft, RoomNumber = "A" },
            new WorkOrder { Number = "WO-002", Title = "B", Status = WorkOrderStatus.Draft, RoomNumber = "B" },
        };
        await using var ctx = CreateContext(new StubBus(rows));

        var component = ctx.Render<WorkOrderSearch>();

        var sortBtn = component.Find($"#{WorkOrderSearch.Elements.SortByRoomButton}");
        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll($"[data-testid^='{WorkOrderSearch.Elements.RoomCell}']");
        cells[0].TextContent.Trim().ShouldBe("C");
        cells[1].TextContent.Trim().ShouldBe("B");
        cells[2].TextContent.Trim().ShouldBe("A");
    }
    [Test]
    public async Task AssignedToMeCheckbox_WhenSessionStateIsTrue_RestoresCheckboxAndFiltersOnLoad()
    {
        // Pre-populate the search state so InitializeAsync restores the AssignedToMe=true branch
        var state = new WorkOrderSearchState { AssignedToMe = true };
        var stubBus = new StubBusWithAssigneeCapture();
        await using var ctx = CreateContext(stubBus, searchState: state);

        var component = ctx.Render<WorkOrderSearch>();

        // Checkbox should be checked (restored from session state)
        var checkbox = component.Find($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        checkbox.HasAttribute("checked").ShouldBeTrue();

        // Assignee select should be disabled
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        assigneeSelect.HasAttribute("disabled").ShouldBeTrue();

        // The query sent to the bus should have the current user's username as assignee
        stubBus.LastAssigneeQueried.ShouldBe(CurrentUsername);
    }

    [Test]
    public async Task AssignedToMeCheckbox_ShouldBeRendered()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var checkbox = component.Find($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        checkbox.ShouldNotBeNull();
        checkbox.HasAttribute("checked").ShouldBeFalse();
    }

    [Test]
    public async Task AssignedToMeCheckbox_WhenChecked_SetsAssigneeToCurrentUser()
    {
        var stubBus = new StubBusWithAssigneeCapture();
        await using var ctx = CreateContext(stubBus);

        var component = ctx.Render<WorkOrderSearch>();

        var checkbox = component.Find($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        await checkbox.ChangeAsync(new() { Value = true });

        // Assignee select should be disabled
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        assigneeSelect.HasAttribute("disabled").ShouldBeTrue();

        // The query sent to the bus should have the current user's username as assignee
        stubBus.LastAssigneeQueried.ShouldBe(CurrentUsername);
    }

    [Test]
    public async Task AssignedToMeCheckbox_WhenUnchecked_ClearsAssigneeFilter()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var checkbox = component.Find($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        // Check then uncheck
        await checkbox.ChangeAsync(new() { Value = true });
        await checkbox.ChangeAsync(new() { Value = false });

        // Assignee dropdown should be re-enabled
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        assigneeSelect.HasAttribute("disabled").ShouldBeFalse();

        // Assignee filter should be cleared
        assigneeSelect.GetAttribute("value").ShouldBeNullOrEmpty();
    }

    [Test]
    public async Task ClearFiltersButton_WhenClicked_ResetsAssignedToMeCheckbox()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var checkbox = component.Find($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        await checkbox.ChangeAsync(new() { Value = true });

        // Clear filters
        var clearButton = component.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        await clearButton.ClickAsync(new());

        // Checkbox should be unchecked
        component.Find($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}").HasAttribute("checked").ShouldBeFalse();

        // Clear button should be disabled (no active filters)
        clearButton.HasAttribute("disabled").ShouldBeTrue();
    }

    [Test]
    public async Task ShouldApply_OverdueRow_CssClass_WhenWorkOrderIsOverdue()
    {
        // An overdue work order: past due date + open status
        var overdueOrder = new WorkOrder
        {
            Number = "WO-OVR",
            Title = "Overdue",
            Status = WorkOrderStatus.InProgress,
            DueDate = new DateOnly(2000, 1, 1)
        };

        await using var ctx = CreateContext(new StubBus([overdueOrder]));

        var component = ctx.Render<WorkOrderSearch>();

        var rows = component.FindAll("tbody tr");
        rows.Count.ShouldBe(1);
        rows[0].ClassName!.ShouldContain("overdue-row");
    }

    [Test]
    public async Task ShouldNotApply_OverdueRow_CssClass_WhenWorkOrderIsNotOverdue()
    {
        var nonOverdueOrder = new WorkOrder
        {
            Number = "WO-FUT",
            Title = "Future",
            Status = WorkOrderStatus.InProgress,
            DueDate = new DateOnly(2099, 12, 31)
        };

        await using var ctx = CreateContext(new StubBus([nonOverdueOrder]));

        var component = ctx.Render<WorkOrderSearch>();

        var rows = component.FindAll("tbody tr");
        rows.Count.ShouldBe(1);
        rows[0].ClassName!.ShouldNotContain("overdue-row");
    }

    [Test]
    public async Task ShouldReport_HasActiveFilters_True_WhenOverdueOnlyIsSet()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        // Toggle the checkbox
        var toggle = component.Find($"#{WorkOrderSearch.Elements.OverdueOnlyToggle}");
        await toggle.ChangeAsync(new() { Value = true });

        var clearButton = component.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        clearButton.HasAttribute("disabled").ShouldBeFalse();
    }

    [Test]
    public async Task ShouldReset_OverdueOnly_OnClearFilters()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();
        var instance = component.Instance;

        // Set the toggle
        var toggle = component.Find($"#{WorkOrderSearch.Elements.OverdueOnlyToggle}");
        await toggle.ChangeAsync(new() { Value = true });
        instance.Model.Filters.OverdueOnly.ShouldBeTrue();

        // Clear filters
        var clearButton = component.Find($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        await clearButton.ClickAsync(new());

        instance.Model.Filters.OverdueOnly.ShouldBeFalse();
    }

    [Test]
    public void ShouldRemotableRequest_RoundTrip_OverdueOnly()
    {
        var query = new ClearMeasure.Bootcamp.Core.Queries.WorkOrderSpecificationQuery();
        query.MatchOverdueOnly(true);

        var json = System.Text.Json.JsonSerializer.Serialize(query);
        var rehydrated = System.Text.Json.JsonSerializer.Deserialize<ClearMeasure.Bootcamp.Core.Queries.WorkOrderSpecificationQuery>(json);

        rehydrated.ShouldNotBeNull();
        rehydrated.OverdueOnly.ShouldBeTrue();
    }
    [Test]
    public async Task ShouldNotRenderSearchButton()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        component.FindAll("#SearchButton").Count.ShouldBe(0);
        component.FindAll("button[type='submit']").Count.ShouldBe(0);
    }

    [Test]
    public async Task ChangingCreator_RunsSearchAndReplacesUrl()
    {
        var bus = new StubBus();
        await using var ctx = CreateContext(bus);
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<WorkOrderSearch>();
        var sendsBefore = bus.SendCallCount;

        await component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}").ChangeAsync(new() { Value = "tlovejoy" });

        bus.SendCallCount.ShouldBeGreaterThan(sendsBefore);
        bus.LastSpecification!.Creator.ShouldNotBeNull();
        navigation.Uri.ShouldEndWith("/workorder/search?Creator=tlovejoy");
        ((BunitNavigationManager)navigation).History.First().Options.ReplaceHistoryEntry.ShouldBeTrue();
    }

    [Test]
    public async Task QueryString_PriorityBuildingAndQ_AppliedOnLoad()
    {
        var bus = new StubBus();
        await using var ctx = CreateContext(bus);
        ctx.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("workorder/search?Priority=High&Building=Kitchen&Q=organ&OpenOnly=true&CompletedWithinDays=30");

        var component = ctx.Render<WorkOrderSearch>();

        bus.LastSpecification!.Priority.ShouldBe(WorkOrderPriority.High);
        bus.LastSpecification.Building.ShouldBe("Kitchen");
        bus.LastSpecification.SearchText.ShouldBe("organ");
        bus.LastSpecification.OpenOnly.ShouldBeTrue();
        bus.LastSpecification.CompletedWithinDays.ShouldBe(30);
        component.Find($"#{WorkOrderSearch.Elements.PrioritySelect}").GetAttribute("value").ShouldBe("High");
        component.Find($"#{WorkOrderSearch.Elements.BuildingSelect}").GetAttribute("value").ShouldBe("Kitchen");
        ChipKeys(component).ShouldBe(["Priority", "Building", "OpenOnly", "CompletedWithinDays", "Q"]);
    }

    [Test]
    public async Task RemovingChip_ClearsThatFilter()
    {
        var bus = new StubBus();
        await using var ctx = CreateContext(bus);
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("workorder/search?Creator=tlovejoy&OpenOnly=true");
        var component = ctx.Render<WorkOrderSearch>();
        component.Find($"[data-testid='{WorkOrderSearch.Elements.ActiveFilterChip}Creator']").TextContent
            .ShouldContain("Creator: Timothy Lovejoy Jr");

        await component.Find($"[data-testid='{WorkOrderSearch.Elements.ActiveFilterChip}OpenOnly']").ClickAsync(new());

        ChipKeys(component).ShouldBe(["Creator"]);
        bus.LastSpecification!.OpenOnly.ShouldBeFalse();
        navigation.Uri.ShouldEndWith("/workorder/search?Creator=tlovejoy");
    }

    [Test]
    public async Task SelectWithValue_HasValueClass()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<WorkOrderSearch>();
        component.Find($"#{WorkOrderSearch.Elements.StatusSelect}").ClassList.ShouldNotContain("has-value");

        await component.Find($"#{WorkOrderSearch.Elements.StatusSelect}").ChangeAsync(new() { Value = WorkOrderStatus.Assigned.Key });

        component.Find($"#{WorkOrderSearch.Elements.StatusSelect}").ClassList.ShouldContain("has-value");
        component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}").ClassList.ShouldNotContain("has-value");
    }

    [Test]
    public async Task ClickingOverdueCard_AppliesOnlyOverdueFilter()
    {
        var bus = new StubBus();
        await using var ctx = CreateContext(bus);
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("workorder/search?Creator=tlovejoy&Priority=Low");
        var component = ctx.Render<WorkOrderSearch>();

        await component.Find("[data-testid='SummaryCardOverdue']").ClickAsync(new());

        bus.LastSpecification!.OverdueOnly.ShouldBeTrue();
        bus.LastSpecification.Creator.ShouldBeNull();
        bus.LastSpecification.Priority.ShouldBeNull();
        component.Find($"#{WorkOrderSearch.Elements.OverdueOnlyToggle}").HasAttribute("checked").ShouldBeTrue();
        navigation.Uri.ShouldEndWith("/workorder/search?OverdueOnly=true");
    }

    [Test]
    public async Task SessionAssignedToMe_OnBareUrl_DoesNotRewriteUrl()
    {
        var bus = new StubBusWithAssigneeCapture();
        await using var ctx = CreateContext(bus, searchState: new WorkOrderSearchState { AssignedToMe = true });
        var navigation = (BunitNavigationManager)ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("workorder/search");
        var historyBefore = navigation.History.Count;

        ctx.Render<WorkOrderSearch>();

        bus.LastAssigneeQueried.ShouldBe(CurrentUsername);
        navigation.Uri.ShouldEndWith("/workorder/search");
        navigation.History.Count.ShouldBe(historyBefore);
    }

    [Test]
    public async Task ShouldRenderNewWorkOrderButton_WhenUserCanCreate()
    {
        var creator = new Employee(CurrentUsername, "Jeffrey", "Palermo", "j@example.com");
        creator.AddRole(new Role("creator", true, false));
        await using var ctx = CreateContext(currentUser: creator);

        var component = ctx.Render<WorkOrderSearch>();

        component.Find($"[data-testid='{WorkOrderSearch.Elements.NewWorkOrderButton}']").GetAttribute("href")
            .ShouldBe("workorder/manage?mode=New");
    }

    [Test]
    public async Task ShouldNotRenderNewWorkOrderButton_WhenUserCannotCreate()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        component.FindAll($"[data-testid='{WorkOrderSearch.Elements.NewWorkOrderButton}']").Count.ShouldBe(0);
    }

    [Test]
    public async Task SavedViewsMenu_TogglesAndClosesOnEscape()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<WorkOrderSearch>();
        var button = component.Find($"[data-testid='{WorkOrderSearch.Elements.SavedViewsButton}']");

        await button.ClickAsync(new());

        component.Find("#saved-views-menu").HasAttribute("hidden").ShouldBeFalse();
        component.FindAll("#saved-views-menu a[role='menuitem']").Select(a => a.GetAttribute("href"))
            .ShouldContain("workorder/search?OverdueOnly=true");

        await component.Find(".menu-anchor").KeyDownAsync(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        component.Find("#saved-views-menu").HasAttribute("hidden").ShouldBeTrue();
    }

    [Test]
    public async Task ShouldRenderSummaryCardsFromSummaryQuery()
    {
        var bus = new StubBus { Summary = new WorkOrderSearchSummary { OpenCount = 24, OverdueCount = 3 } };
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<WorkOrderSearch>();

        component.Find("[data-testid='SummaryCardOpen'] .summary-card-value").TextContent.ShouldBe("24");
        component.Find("[data-testid='SummaryCardOverdue'] .summary-card-value").TextContent.ShouldBe("3");
    }

    [Test]
    public async Task ShouldRenderColumnsInMockupOrder()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        component.FindAll("thead th").Select(th => th.TextContent.Trim())
            .ShouldBe(["", "Work Order", "Title", "Status", "Priority", "Assignee", "Room", "Due Date", "Actions"]);
    }

    [Test]
    public async Task ShouldShowDashAndHiddenText_WhenNoDueDate()
    {
        var order = new WorkOrder { Number = "WO-ND", Title = "No date", Status = WorkOrderStatus.Draft, DueDate = null };
        await using var ctx = CreateContext(new StubBus([order]));

        var component = ctx.Render<WorkOrderSearch>();

        var cell = component.Find($"[data-testid='{WorkOrderSearch.Elements.DueDateCell}WO-ND']");
        cell.TextContent.ShouldBe("–");
        cell.ParentElement!.QuerySelector(".visually-hidden")!.TextContent.ShouldBe("No due date");
    }

    [Test]
    public async Task ShouldShowUnassigned_WhenNoAssignee()
    {
        var order = new WorkOrder { Number = "WO-UA", Title = "Nobody", Status = WorkOrderStatus.Draft };
        await using var ctx = CreateContext(new StubBus([order]));

        var component = ctx.Render<WorkOrderSearch>();

        var cell = component.Find($"[data-testid='{WorkOrderSearch.Elements.AssigneeCell}WO-UA']");
        cell.TextContent.Trim().ShouldBe("Unassigned");
        cell.QuerySelector(".avatar--empty").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldShowAssigneeInitialsAndPriorityLabel()
    {
        var order = new WorkOrder
        {
            Number = "WO-AS", Title = "Organ", Status = WorkOrderStatus.Assigned, Priority = WorkOrderPriority.High,
            Assignee = new Employee("gwillie", "Groundskeeper", "Willie", "g@x")
        };
        await using var ctx = CreateContext(new StubBus([order]));

        var component = ctx.Render<WorkOrderSearch>();

        component.Find($"[data-testid='{WorkOrderSearch.Elements.AssigneeCell}WO-AS'] .avatar").TextContent.ShouldBe("GW");
        var priority = component.Find($"[data-testid='{WorkOrderSearch.Elements.PriorityCell}WO-AS'] .priority");
        priority.ClassList.ShouldContain("priority--high");
        priority.TextContent.Trim().ShouldBe("High");
        component.Find($"[data-testid='{WorkOrderSearch.Elements.StatusCell}WO-AS'] .status-badge").TextContent.ShouldBe("Assigned");
    }

    [Test]
    public async Task ShouldShowBulkBar_WhenRowSelected()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<WorkOrderSearch>();
        component.FindAll("[data-testid='BulkActionBar']").Count.ShouldBe(0);

        await component.Find($"[data-testid='{WorkOrderSearch.Elements.RowCheckbox}WO-001']").ChangeAsync(new() { Value = true });

        var bar = component.Find("[data-testid='BulkActionBar']");
        bar.GetAttribute("role").ShouldBe("toolbar");
        bar.QuerySelector("[data-testid='BulkSelectedCount']")!.TextContent.ShouldBe("1 selected");
        component.Find("tbody tr").ClassList.ShouldContain("row-selected");
        component.Find($"[data-testid='{WorkOrderSearch.Elements.ResultsRangeText}']").TextContent.ShouldEndWith("1 selected");
    }

    [Test]
    public async Task SelectAll_SelectsCurrentPageOnly()
    {
        await using var ctx = CreateContext(new StubBus(ManyOrders(30)));
        var component = ctx.Render<WorkOrderSearch>();

        await component.Find($"#{WorkOrderSearch.Elements.SelectAllCheckbox}").ChangeAsync(new() { Value = true });

        component.Find("[data-testid='BulkSelectedCount']").TextContent.ShouldBe("25 selected");
        component.Find($"#{WorkOrderSearch.Elements.SelectAllCheckbox}").HasAttribute("checked").ShouldBeTrue();
    }

    [Test]
    public async Task ShouldShowSecondPage_WhenNextClicked()
    {
        await using var ctx = CreateContext(new StubBus(ManyOrders(30)));
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<WorkOrderSearch>();
        component.FindAll("tbody tr").Count.ShouldBe(25);
        component.Find($"[data-testid='{WorkOrderSearch.Elements.PagePrevButton}']").HasAttribute("disabled").ShouldBeTrue();
        component.Find($"[data-testid='{WorkOrderSearch.Elements.PageButton}1']").GetAttribute("aria-current").ShouldBe("page");

        await component.Find($"[data-testid='{WorkOrderSearch.Elements.PageNextButton}']").ClickAsync(new());

        component.FindAll("tbody tr").Count.ShouldBe(5);
        component.Find($"[data-testid='{WorkOrderSearch.Elements.ResultsCountPill}']").TextContent.ShouldBe("5 of 30");
        component.Find($"[data-testid='{WorkOrderSearch.Elements.ResultsRangeText}']").TextContent
            .ShouldBe("Showing 26–30 of 30 work orders · 0 selected");
        component.Find($"[data-testid='{WorkOrderSearch.Elements.PageNextButton}']").HasAttribute("disabled").ShouldBeTrue();
        navigation.Uri.ShouldEndWith("/workorder/search?Page=2");
    }

    [Test]
    public async Task SortHeader_SetsAriaSort()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<WorkOrderSearch>();
        var priorityHeader = () => component.Find($"#{WorkOrderSearch.Elements.SortByPriorityButton}").ParentElement!;
        priorityHeader().GetAttribute("aria-sort").ShouldBe("none");

        await component.Find($"#{WorkOrderSearch.Elements.SortByPriorityButton}").ClickAsync(new());
        priorityHeader().GetAttribute("aria-sort").ShouldBe("ascending");

        await component.Find($"#{WorkOrderSearch.Elements.SortByPriorityButton}").ClickAsync(new());
        priorityHeader().GetAttribute("aria-sort").ShouldBe("descending");
        component.Find($"#{WorkOrderSearch.Elements.SortSelect}").QuerySelector("option[selected]")!.TextContent
            .ShouldBe("Sort: Priority ↓");
    }

    [Test]
    public async Task SortSelect_SortsLikeTheHeader()
    {
        var rows = new[]
        {
            new WorkOrder { Number = "WO-L", Title = "L", Status = WorkOrderStatus.Draft, Priority = WorkOrderPriority.Low },
            new WorkOrder { Number = "WO-H", Title = "H", Status = WorkOrderStatus.Draft, Priority = WorkOrderPriority.High },
            new WorkOrder { Number = "WO-M", Title = "M", Status = WorkOrderStatus.Draft, Priority = WorkOrderPriority.Medium }
        };
        await using var ctx = CreateContext(new StubBus(rows));
        var component = ctx.Render<WorkOrderSearch>();

        await component.Find($"#{WorkOrderSearch.Elements.SortSelect}").ChangeAsync(new() { Value = "Priority:desc" });

        LinkNumbers(component).ShouldBe(["WO-H", "WO-M", "WO-L"]);
        component.Find($"#{WorkOrderSearch.Elements.SortByPriorityButton}").TextContent.ShouldContain("▼");

        await component.Find($"#{WorkOrderSearch.Elements.SortSelect}").ChangeAsync(new() { Value = "" });

        LinkNumbers(component).ShouldBe(["WO-L", "WO-H", "WO-M"]);
    }

    [Test]
    public async Task ShouldPrefixDisplayNumberButKeepHrefAndTestId()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<WorkOrderSearch>();

        var link = component.Find($"[data-testid='{WorkOrderSearch.Elements.WorkOrderLink}WO-001']");
        link.TextContent.ShouldBe("WO-WO-001");
        link.GetAttribute("href").ShouldBe("/workorder/manage/WO-001?mode=Edit");
    }

    [Test]
    public async Task ShouldShowOverduePillWithHiddenStoredStatus()
    {
        var order = new WorkOrder { Number = "WO-OD", Title = "Late", Status = WorkOrderStatus.InProgress, DueDate = new DateOnly(2000, 1, 1) };
        await using var ctx = CreateContext(new StubBus([order]));

        var component = ctx.Render<WorkOrderSearch>();

        var cell = component.Find($"[data-testid='{WorkOrderSearch.Elements.StatusCell}WO-OD']");
        cell.QuerySelector(".status-badge")!.TextContent.ShouldBe("Overdue");
        cell.QuerySelector(".status-badge")!.ClassList.ShouldContain("status-badge--overdue");
        cell.QuerySelector(".visually-hidden")!.TextContent.ShouldBe(", status In Progress");
    }

    [Test]
    public async Task RowMenu_OpensWithOpenLink()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<WorkOrderSearch>();

        await component.Find($"[data-testid='{WorkOrderSearch.Elements.RowMenu}WO-001']").ClickAsync(new());

        component.Find($"[data-testid='{WorkOrderSearch.Elements.RowMenuOpen}WO-001']").GetAttribute("href")
            .ShouldBe("/workorder/manage/WO-001?mode=Edit");
        await component.Find(".col-actions .menu-anchor").KeyDownAsync(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        component.FindAll($"[data-testid='{WorkOrderSearch.Elements.RowMenuOpen}WO-001']").Count.ShouldBe(0);
    }

    [Test]
    public async Task Export_DownloadsCsvOfFilteredRows()
    {
        await using var ctx = CreateContext();
        var module = ctx.JSInterop.SetupModule("./_content/ClearMeasure.Bootcamp.UI.Shared/js/searchExport.js");
        module.SetupVoid("downloadCsv", _ => true).SetVoidResult();
        module.SetupVoid("setIndeterminate", _ => true).SetVoidResult();
        var component = ctx.Render<WorkOrderSearch>();

        await component.Find($"[data-testid='{WorkOrderSearch.Elements.ExportButton}']").ClickAsync(new());

        var invocation = module.VerifyInvoke("downloadCsv");
        invocation.Arguments[0]!.ToString()!.ShouldStartWith("work-orders-");
        var csv = invocation.Arguments[1]!.ToString()!;
        csv.ShouldContain("WO-001");
        csv.ShouldContain("WO-002");
    }

    [Test]
    public async Task BulkAssign_SendsAssignForSelectedRowsAndReportsOutcome()
    {
        var creator = new Employee(CurrentUsername, "Jeffrey", "Palermo", "j@example.com") { Id = Guid.NewGuid() };
        creator.AddRole(new Role("creator", true, false));
        var draft = new WorkOrder { Number = "WO-D1", Title = "Draft", Status = WorkOrderStatus.Draft, Creator = creator };
        var bus = new StubBulkBus([draft]);
        await using var ctx = CreateContext(bus, currentUser: creator);
        var component = ctx.Render<WorkOrderSearch>();

        await component.Find($"[data-testid='{WorkOrderSearch.Elements.RowCheckbox}WO-D1']").ChangeAsync(new() { Value = true });
        await component.Find("[data-testid='BulkAssignSelect']").ChangeAsync(new() { Value = "hsimpson" });
        await component.Find("[data-testid='BulkAssignButton']").ClickAsync(new());

        bus.Commands.Single().TransitionVerbPresentTense.ShouldBe("Assign");
        bus.Commands.Single().WorkOrder.Assignee!.UserName.ShouldBe("hsimpson");
        component.Find($"[data-testid='{WorkOrderSearch.Elements.BulkOutcome}']").TextContent.ShouldBe("1 updated, 0 skipped");
        component.FindAll("[data-testid='BulkActionBar']").Count.ShouldBe(0);
    }

    private static WorkOrder[] ManyOrders(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new WorkOrder { Number = $"WO-{i:000}", Title = $"Order {i}", Status = WorkOrderStatus.Draft })
            .ToArray();

    private sealed class StubBulkBus : StubBus
    {
        private readonly WorkOrder[] _orders;

        public StubBulkBus(WorkOrder[] orders) : base(orders)
        {
            _orders = orders;
        }

        public List<ClearMeasure.Bootcamp.Core.Model.StateCommands.StateCommandBase> Commands { get; } = [];

        public override Task<TResponse> Send<TResponse>(MediatR.IRequest<TResponse> request)
        {
            if (request is WorkOrderByNumberQuery byNumber)
            {
                return Task.FromResult((TResponse)(object)_orders.Single(o => o.Number == byNumber.Number));
            }

            if (request is ClearMeasure.Bootcamp.Core.Model.StateCommands.StateCommandBase command)
            {
                Commands.Add(command);
                return Task.FromResult((TResponse)(object)new ClearMeasure.Bootcamp.Core.Model.StateCommands.StateCommandResult(command.WorkOrder, command.TransitionVerbPresentTense, "done"));
            }

            return base.Send(request);
        }
    }

    private static string[] LinkNumbers(IRenderedComponent<WorkOrderSearch> component) =>
        component.FindAll($"[data-testid^='{WorkOrderSearch.Elements.WorkOrderLink}']")
            .Select(a => a.GetAttribute("data-testid")![nameof(WorkOrderSearch.Elements.WorkOrderLink).Length..])
            .ToArray();

    private static string[] ChipKeys(IRenderedComponent<WorkOrderSearch> component) =>
        component.FindAll($"[data-testid^='{WorkOrderSearch.Elements.ActiveFilterChip}']")
            .Select(c => c.GetAttribute("data-testid")![nameof(WorkOrderSearch.Elements.ActiveFilterChip).Length..])
            .ToArray();
}
