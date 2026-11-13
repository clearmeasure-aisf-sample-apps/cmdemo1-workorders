using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[Parallelizable(ParallelScope.All)]
public class WorkOrderSearchTests : WorkOrderAcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task Should_PreserveMixedCaseNames_InWorkOrderSearchDropdowns()
    {
        await Click(nameof(NavMenu.Elements.Search));

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");

        await Expect(creatorSelect.Locator("option").Filter(new() { HasText = "Timothy Lovejoy" })).ToHaveCountAsync(1);
        await Expect(assigneeSelect.Locator("option").Filter(new() { HasText = "Timothy Lovejoy" })).ToHaveCountAsync(1);

        var creatorTexts = await creatorSelect.Locator("option").AllInnerTextsAsync();
        var assigneeTexts = await assigneeSelect.Locator("option").AllInnerTextsAsync();

        creatorTexts.ShouldNotContain("TIMOTHY LOVEJOY JR");
        assigneeTexts.ShouldNotContain("TIMOTHY LOVEJOY JR");
    }

    [Test, Retry(2)]
    public async Task ShouldAssociateFilterLabelsWithSelectIds()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        await Expect(Page.Locator($"label[for='{WorkOrderSearch.Elements.CreatorSelect}']")).ToBeAttachedAsync();
        await Expect(Page.Locator($"label[for='{WorkOrderSearch.Elements.AssigneeSelect}']")).ToBeAttachedAsync();
        await Expect(Page.Locator($"label[for='{WorkOrderSearch.Elements.StatusSelect}']")).ToBeAttachedAsync();

        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}")).ToBeVisibleAsync();
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}")).ToBeVisibleAsync();
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}")).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldLoadDropDownsInitiallyOnLoad()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await TakeScreenshotAsync(1, "PageLoaded");

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}");

        await Expect(creatorSelect).ToBeVisibleAsync();
        await Expect(assigneeSelect).ToBeVisibleAsync();
        await Expect(statusSelect).ToBeVisibleAsync();

        const int minimumBaseEmployees = 18;
        var creatorOptions = creatorSelect.Locator("option");
        await Expect(creatorOptions.First).ToHaveTextAsync("Anyone");
        await Expect(creatorOptions.Filter(new() { HasText = "Timothy Lovejoy" })).ToHaveCountAsync(1);
        var creatorOptionCount = await creatorOptions.CountAsync();
        creatorOptionCount.ShouldBeGreaterThanOrEqualTo(minimumBaseEmployees + 1);

        var assigneeOptions = assigneeSelect.Locator("option");
        await Expect(assigneeOptions.First).ToHaveTextAsync("Anyone");
        await Expect(assigneeOptions.Filter(new() { HasText = "Timothy Lovejoy" })).ToHaveCountAsync(1);
        var assigneeOptionCount = await assigneeOptions.CountAsync();
        assigneeOptionCount.ShouldBeGreaterThanOrEqualTo(minimumBaseEmployees + 1);

        var statusOptions = statusSelect.Locator("option");
        await Expect(statusOptions).ToHaveCountAsync(WorkOrderStatus.GetAllItems().Length + 1);
        await Expect(statusOptions.First).ToHaveTextAsync("All statuses");
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderTableWithAllFiltersSetToAllOnInitialLoad()
    {
        var creator = await SeedEmployeeAsync();
        var assignee = await SeedEmployeeAsync();
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Assignee = assignee, Title = $"[{TestTag}] initial load 1" });
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Assignee = assignee, Title = $"[{TestTag}] initial load 2" });

        await Click(nameof(NavMenu.Elements.Search));
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(1, "InitialLoad");

        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable).ToBeVisibleAsync();

        var workOrderRows = workOrderTable.Locator("tbody tr");
        var rowCount = await workOrderRows.CountAsync();
        await Expect(workOrderRows).ToHaveCountAsync(rowCount);
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderTableWithCreatorFilterFromQueryString()
    {
        var creator = CurrentUser;
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Title = $"[{TestTag}] search test" });

        await Click(nameof(NavMenu.Elements.MyWorkOrders));
        await TakeScreenshotAsync(1, "CreatorFiltered");

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        await Expect(creatorSelect).ToHaveValueAsync(creator.UserName);

        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable).ToBeVisibleAsync();

        var workOrderRows = workOrderTable.Locator("tbody tr");
        var rowCount = await workOrderRows.CountAsync();
        rowCount.ShouldBeGreaterThanOrEqualTo(1);
        await Expect(Page.GetByTestId($"{WorkOrderSearch.Elements.ActiveFilterChip}Creator")).ToContainTextAsync(creator.DisplayName);
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderTableWithAssigneeFilterFromQueryString()
    {
        var creator = await SeedEmployeeAsync();
        var assignee = CurrentUser;
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Status = WorkOrderStatus.Assigned, Assignee = assignee, Title = $"[{TestTag}] assignee test" });

        await Click(nameof(NavMenu.Elements.WorkOrdersAssignedToMe));
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(1, "AssigneeFiltered");

        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        await Expect(assigneeSelect).ToHaveValueAsync(assignee.UserName);

        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable).ToBeVisibleAsync();

        var workOrderRows = workOrderTable.Locator("tbody tr");
        var rowCount = await workOrderRows.CountAsync();
        rowCount.ShouldBeGreaterThanOrEqualTo(1);
        await Expect(FirstRowCell(WorkOrderSearch.Elements.AssigneeCell)).ToContainTextAsync(assignee.DisplayName);
    }

    [Test, Retry(2)]
    public async Task ShouldLoadWorkOrderTableWithStatusFilterFromQueryString()
    {
        var creator = await SeedEmployeeAsync();
        var status = WorkOrderStatus.Assigned;
        var assignee = await SeedEmployeeAsync();
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Status = status, Assignee = assignee, Title = $"[{TestTag}] status filter" });

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.EvaluateAsync("u => Blazor.navigateTo(u)", $"/workorder/search?Status={status.Key}");
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(1, "StatusFiltered");

        var statusSelect = Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}");
        await Expect(statusSelect).ToHaveValueAsync(status.Key);

        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable).ToBeVisibleAsync();

        var workOrderRows = workOrderTable.Locator("tbody tr");
        await Expect(workOrderRows).ToHaveCountAsync(await workOrderRows.CountAsync());
        await Expect(FirstRowCell(WorkOrderSearch.Elements.StatusCell)).ToContainTextAsync(status.FriendlyName);
    }

    [Test, Retry(2)]
    public async Task ShouldSearchWithAllThreeFiltersSelected()
    {
        var creator = await SeedEmployeeAsync();
        var assignee = await SeedEmployeeAsync();
        var status = WorkOrderStatus.Assigned;
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Status = status, Assignee = assignee, Title = $"[{TestTag}] three filters" });

        await Click(nameof(NavMenu.Elements.Search));
        await TakeScreenshotAsync(1, "BeforeFiltering");

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}");

        await creatorSelect.SelectOptionAsync(creator.UserName);
        await assigneeSelect.SelectOptionAsync(assignee.UserName);
        await statusSelect.SelectOptionAsync(status.Key);
        await TakeScreenshotAsync(2, "FiltersSet");
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(3, "SearchCompleted");

        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable).ToBeVisibleAsync();

        await workOrderTable.Locator("tbody").WaitForAsync();

        var workOrderRows = workOrderTable.Locator("tbody tr");
        await Expect(workOrderRows).ToHaveCountAsync(1);
    }

    [Test, Retry(2)]
    public async Task ShouldNavigateToWorkOrderDetailsWhenClickingWorkOrderNumber()
    {
        var creator = await SeedEmployeeAsync();
        var workOrder = await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Title = $"[{TestTag}] details" });

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.EvaluateAsync("u => Blazor.navigateTo(u)", $"/workorder/search?Q={TestTag}");
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(1, "SearchPageLoaded");

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + workOrder.Number);
        await Expect(workOrderLink).ToBeVisibleAsync();
        await workOrderLink.ClickAsync();
        await Page.WaitForURLAsync(new Regex($"/workorder/manage/{Regex.Escape(workOrder.Number!)}"));
        await TakeScreenshotAsync(2, "WorkOrderDetailsPage");

        await Expect(Page).ToHaveURLAsync(new Regex($"/workorder/manage/{Regex.Escape(workOrder.Number!)}"));
    }

    [Test, Retry(2)]
    public async Task ShouldClearFiltersWhenSelectingAllOption()
    {
        var creator = await SeedEmployeeAsync();
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Title = $"[{TestTag}] clear all option" });

        await Click(nameof(NavMenu.Elements.Search));

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");

        await creatorSelect.SelectOptionAsync(creator.UserName);
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(1, "FilterSet");

        await creatorSelect.SelectOptionAsync("");
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(2, "FilterCleared");

        await Expect(creatorSelect).ToHaveValueAsync("");

        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldMaintainSelectedFiltersAfterSearch()
    {
        var creator = await SeedEmployeeAsync();
        var assignee = await SeedEmployeeAsync();
        var status = WorkOrderStatus.Assigned;
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Status = status, Assignee = assignee, Title = $"[{TestTag}] maintain filters" });

        await Click(nameof(NavMenu.Elements.Search));

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}");

        await creatorSelect.SelectOptionAsync(creator.UserName);
        await assigneeSelect.SelectOptionAsync(assignee.UserName);
        await statusSelect.SelectOptionAsync(status.Key);
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(1, "AfterSearch");

        await Expect(creatorSelect).ToHaveValueAsync(creator.UserName);
        await Expect(assigneeSelect).ToHaveValueAsync(assignee.UserName);
        await Expect(statusSelect).ToHaveValueAsync(status.Key);
    }

    [Test, Retry(2)]
    public async Task ShouldClearAllFilters_WhenClearFiltersButtonClicked()
    {
        var creator = CurrentUser;
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Title = $"[{TestTag}] clear filters test" });

        await Click(nameof(NavMenu.Elements.Search));
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}");
        var clearButton = Page.Locator($"#{WorkOrderSearch.Elements.ClearFiltersButton}");

        await creatorSelect.SelectOptionAsync(creator.UserName);
        await TakeScreenshotAsync(1, "FilterSet");

        await Expect(clearButton).ToBeEnabledAsync();

        await clearButton.ClickAsync();
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(2, "FiltersCleared");

        await Expect(creatorSelect).ToHaveValueAsync("");
        await Expect(assigneeSelect).ToHaveValueAsync("");
        await Expect(statusSelect).ToHaveValueAsync("");

        await Expect(Page.Locator(".grid-data")).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ClearFiltersButton_ShouldBeDisabled_WhenNoFiltersAreActive()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(1, "PageLoaded");

        var clearButton = Page.Locator($"#{WorkOrderSearch.Elements.ClearFiltersButton}");
        await Expect(clearButton).ToBeDisabledAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldReloadParamsFromQueryStringWithNavigation()
    {
        await SeedWorkOrderAsync(CurrentUser, new WorkOrderSeedSpec { Status = WorkOrderStatus.InProgress, Assignee = CurrentUser, Title = $"[{TestTag}] reload params 1" });
        await SeedWorkOrderAsync(CurrentUser, new WorkOrderSeedSpec { Assignee = CurrentUser, Title = $"[{TestTag}] reload params 2" });

        await Click(nameof(NavMenu.Elements.Search));
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}");

        await Expect(creatorSelect).ToHaveValueAsync("");
        await Expect(assigneeSelect).ToHaveValueAsync("");
        await Expect(statusSelect).ToHaveValueAsync("");

        await Click(nameof(NavMenu.Elements.MyWorkOrders));
        await Expect(creatorSelect).ToHaveValueAsync(CurrentUser.UserName, new() { Timeout = 30_000 });

        await Click(nameof(NavMenu.Elements.WorkOrdersAssignedToMe));
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await Expect(assigneeSelect).ToHaveValueAsync(CurrentUser.UserName, new() { Timeout = 30_000 });

        await Click(nameof(NavMenu.Elements.AllWorkOrdersInProgress));
        await Expect(statusSelect).ToHaveValueAsync(WorkOrderStatus.InProgress.Key, new() { Timeout = 30_000 });
    }

    [Test, Retry(2)]
    public async Task SortByStatusHeader_SortsResultsAscending_ThenDescendingOnSecondClick()
    {
        var creator = await SeedEmployeeAsync();
        var assignee = await SeedEmployeeAsync();
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Status = WorkOrderStatus.InProgress, Assignee = assignee, Title = $"[{TestTag}] sort status in progress" });
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Status = WorkOrderStatus.Assigned, Assignee = assignee, Title = $"[{TestTag}] sort status assigned" });

        await Click(nameof(NavMenu.Elements.Search));
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(1, "Loaded");

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        await creatorSelect.SelectOptionAsync(creator.UserName);
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(2, "Filtered");

        var statusSortBtn = Page.Locator($"#{WorkOrderSearch.Elements.SortByStatusButton}");
        await statusSortBtn.ClickAsync();
        await TakeScreenshotAsync(3, "SortedAscending");

        await Expect(statusSortBtn).ToHaveTextAsync("Status ▲");
        var firstCell = FirstRowCell(WorkOrderSearch.Elements.StatusCell);
        await Expect(firstCell).ToContainTextAsync(WorkOrderStatus.Assigned.FriendlyName);

        await statusSortBtn.ClickAsync();
        await TakeScreenshotAsync(4, "SortedDescending");

        await Expect(statusSortBtn).ToHaveTextAsync("Status ▼");
        await Expect(firstCell).ToContainTextAsync(WorkOrderStatus.InProgress.FriendlyName);
    }

    [Test, Retry(2)]
    public async Task SortByTitleHeader_SortsResultsAscending_ThenDescendingOnSecondClick()
    {
        var creator = await SeedEmployeeAsync();
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Title = "Alpha" });
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Title = "Zebra" });

        await Click(nameof(NavMenu.Elements.Search));

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        await creatorSelect.SelectOptionAsync(creator.UserName);
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        var titleSortBtn = Page.Locator($"#{WorkOrderSearch.Elements.SortByTitleButton}");
        await titleSortBtn.ClickAsync();
        await TakeScreenshotAsync(1, "SortedAscending");

        await Expect(titleSortBtn).ToHaveTextAsync("Title ▲");
        var firstTitleCell = FirstRowCell(WorkOrderSearch.Elements.TitleCell);
        await Expect(firstTitleCell).ToContainTextAsync("Alpha");

        await titleSortBtn.ClickAsync();
        await TakeScreenshotAsync(2, "SortedDescending");

        await Expect(titleSortBtn).ToHaveTextAsync("Title ▼");
        await Expect(firstTitleCell).ToContainTextAsync("Zebra");
    }

    [Test, Retry(2)]
    public async Task SortByRoomHeader_SortsResultsAscending_ThenDescendingOnSecondClick()
    {
        var creator = await SeedEmployeeAsync();
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Title = $"[{TestTag}] room atrium", RoomNumber = "Atrium" });
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Title = $"[{TestTag}] room zeppelin", RoomNumber = "Zeppelin" });

        await Click(nameof(NavMenu.Elements.Search));
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        await creatorSelect.SelectOptionAsync(creator.UserName);
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        var roomSortBtn = Page.Locator($"#{WorkOrderSearch.Elements.SortByRoomButton}");
        await roomSortBtn.ClickAsync();
        await TakeScreenshotAsync(1, "SortedAscending");

        await Expect(roomSortBtn).ToHaveTextAsync("Room ▲");
        var firstRoomCell = FirstRowCell(WorkOrderSearch.Elements.RoomCell);
        await Expect(firstRoomCell).ToContainTextAsync("Atrium");

        await roomSortBtn.ClickAsync();
        await TakeScreenshotAsync(2, "SortedDescending");

        await Expect(roomSortBtn).ToHaveTextAsync("Room ▼");
        await Expect(firstRoomCell).ToContainTextAsync("Zeppelin");
    }

    [Test, Retry(2)]
    public async Task AssignedToMe_WhenChecked_ShowsOnlyCurrentUsersWorkOrders()
    {
        var otherEmployee = await SeedEmployeeAsync();
        var myOrder = await SeedWorkOrderAsync(CurrentUser, new WorkOrderSeedSpec { Status = WorkOrderStatus.Assigned, Assignee = CurrentUser, Title = $"[{TestTag}] my order" });
        var otherOrder = await SeedWorkOrderAsync(otherEmployee, new WorkOrderSeedSpec { Status = WorkOrderStatus.Assigned, Assignee = otherEmployee, Title = $"[{TestTag}] other order" });

        await OpenSearchScopedToTestTag();
        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable.Locator("tbody tr").Filter(new() { HasText = myOrder.Title })).ToHaveCountAsync(1);
        await Expect(workOrderTable.Locator("tbody tr").Filter(new() { HasText = otherOrder.Title })).ToHaveCountAsync(1);
        await TakeScreenshotAsync(1, "BeforeToggle");

        var checkbox = Page.Locator($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        await checkbox.CheckAsync();
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(2, "AfterToggle");

        await Expect(workOrderTable).ToBeVisibleAsync();
        await Expect(workOrderTable.Locator("tbody tr").Filter(new() { HasText = myOrder.Title })).ToHaveCountAsync(1);
        await Expect(workOrderTable.Locator("tbody tr").Filter(new() { HasText = otherOrder.Title })).ToHaveCountAsync(0);
        await Expect(workOrderTable.Locator("tbody tr")).ToHaveCountAsync(1);

        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        await Expect(assigneeSelect).ToBeDisabledAsync();
    }

    [Test, Retry(2)]
    public async Task AssignedToMe_WhenUnchecked_RestoresFullList()
    {
        var otherEmployee = await SeedEmployeeAsync();
        var myOrder = await SeedWorkOrderAsync(CurrentUser, new WorkOrderSeedSpec { Status = WorkOrderStatus.Assigned, Assignee = CurrentUser, Title = $"[{TestTag}] my order" });
        var otherOrder = await SeedWorkOrderAsync(otherEmployee, new WorkOrderSeedSpec { Status = WorkOrderStatus.Assigned, Assignee = otherEmployee, Title = $"[{TestTag}] other order" });

        await OpenSearchScopedToTestTag();
        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable.Locator("tbody tr").Filter(new() { HasText = myOrder.Title })).ToHaveCountAsync(1);
        await Expect(workOrderTable.Locator("tbody tr").Filter(new() { HasText = otherOrder.Title })).ToHaveCountAsync(1);

        var checkbox = Page.Locator($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        await checkbox.CheckAsync();
        await Expect(workOrderTable.Locator("tbody tr").Filter(new() { HasText = otherOrder.Title })).ToHaveCountAsync(0);
        await TakeScreenshotAsync(1, "ToggleOn");

        await checkbox.UncheckAsync();
        await TakeScreenshotAsync(2, "ToggleOff");

        await Expect(workOrderTable.Locator("tbody tr").Filter(new() { HasText = myOrder.Title })).ToHaveCountAsync(1);
        await Expect(workOrderTable.Locator("tbody tr").Filter(new() { HasText = otherOrder.Title })).ToHaveCountAsync(1);

        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        await Expect(assigneeSelect).ToBeEnabledAsync();
    }

    [Test, Retry(2)]
    public async Task AssignedToMe_ToggleStatePersistedWithinSession()
    {
        var myOrder = await SeedWorkOrderAsync(CurrentUser, new WorkOrderSeedSpec { Status = WorkOrderStatus.Assigned, Assignee = CurrentUser, Title = $"[{TestTag}] persisted order" });

        await OpenSearchScopedToTestTag();
        await Expect(Page.Locator(".grid-data tbody tr").Filter(new() { HasText = myOrder.Title })).ToHaveCountAsync(1);

        var checkbox = Page.Locator($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        await checkbox.CheckAsync();
        await TakeScreenshotAsync(1, "ToggleChecked");

        var workOrderLink = Page.Locator(".grid-data tbody tr").Filter(new() { HasText = myOrder.Title })
            .Locator($"[data-testid^='{WorkOrderSearch.Elements.WorkOrderLink}']").First;
        await workOrderLink.ClickAsync();
        await TakeScreenshotAsync(2, "WorkOrderDetail");

        await Click(nameof(NavMenu.Elements.Search));
        await TakeScreenshotAsync(3, "BackOnSearch");

        var checkboxAfterNav = Page.Locator($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}");
        await Expect(checkboxAfterNav).ToBeCheckedAsync();

        var workOrderTable = Page.Locator(".grid-data");
        await Expect(workOrderTable.Locator("tbody tr").Filter(new() { HasText = myOrder.Title })).ToHaveCountAsync(1);
    }

    /// <summary>
    /// Opens Work Order Search filtered by this test's tag, so the list holds only rows this test
    /// titled with <c>[TestTag]</c>. The tag is passed without brackets: SQL Server LIKE reads them
    /// as a character class.
    /// </summary>
    private async Task OpenSearchScopedToTestTag()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.EvaluateAsync("u => Blazor.navigateTo(u)", $"/workorder/search?Q={TestTag}");
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
    }

    [Test, Retry(2)]
    public async Task SortByDueDateHeader_SortsResultsAscending_ThenDescendingOnSecondClick()
    {
        var creator = await SeedEmployeeAsync();
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { DueDate = new DateOnly(2099, 1, 1), Title = $"[{TestTag}] due early" });
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { DueDate = new DateOnly(2099, 12, 31), Title = $"[{TestTag}] due late" });

        await Click(nameof(NavMenu.Elements.Search));
        await TakeScreenshotAsync(1, "Loaded");

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        await creatorSelect.SelectOptionAsync(creator.UserName);
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(2, "Filtered");

        var dueDateSortBtn = Page.Locator($"#{WorkOrderSearch.Elements.SortByDueDateButton}");
        await dueDateSortBtn.ClickAsync();
        await TakeScreenshotAsync(3, "SortedAscending");

        await Expect(dueDateSortBtn).ToHaveTextAsync("Due Date ▲");
        var firstDueDate = FirstRowCell(WorkOrderSearch.Elements.DueDateCell);
        await Expect(firstDueDate).ToContainTextAsync("1/1/2099");

        await dueDateSortBtn.ClickAsync();
        await TakeScreenshotAsync(4, "SortedDescending");

        await Expect(dueDateSortBtn).ToHaveTextAsync("Due Date ▼");
        await Expect(firstDueDate).ToContainTextAsync("12/31/2099");
    }

    [Test, Retry(2)]
    public async Task Should_ShowOverdueOnly_WhenToggleIsChecked()
    {
        var creator = await SeedEmployeeAsync();
        var assignee = await SeedEmployeeAsync();
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Status = WorkOrderStatus.InProgress, Assignee = assignee, DueDate = new DateOnly(2000, 1, 1), Title = $"[{TestTag}] overdue" });
        await SeedWorkOrderAsync(creator, new WorkOrderSeedSpec { Status = WorkOrderStatus.InProgress, Assignee = assignee, DueDate = new DateOnly(2099, 12, 31), Title = $"[{TestTag}] future" });

        await Click(nameof(NavMenu.Elements.Search));
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        await creatorSelect.SelectOptionAsync(creator.UserName);
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(1, "BothOrders");

        var tableRows = Page.Locator(".grid-data tbody tr");
        await Expect(tableRows).ToHaveCountAsync(2);

        var toggle = Page.Locator($"#{WorkOrderSearch.Elements.OverdueOnlyToggle}");
        await toggle.CheckAsync();
        await Page.Locator(".grid-data tbody").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(2, "OverdueOnly");

        await Expect(tableRows).ToHaveCountAsync(1);
        var onlyRow = tableRows.First;
        await Expect(onlyRow).ToHaveClassAsync(new Regex("overdue-row"));
    }

    private ILocator FirstRowCell(WorkOrderSearch.Elements cell) =>
        Page.Locator(".grid-data tbody tr").First.Locator($"[data-testid^='{cell}']");
}
