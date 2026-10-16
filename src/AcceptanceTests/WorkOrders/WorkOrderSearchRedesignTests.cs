using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSearchRedesignTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task ShouldShowEachWorkOrderOnce_WhenFilteredByCreator()
    {
        var creator = Faker<Employee>();
        creator.UserName = $"dup_{TestTag}";
        creator.AddRole(new Role($"Pastor_{TestTag}", true, false));
        creator.AddRole(new Role($"Requester_{TestTag}", true, false));
        var orders = Enumerable.Range(0, 3).Select(_ =>
        {
            var order = Faker<WorkOrder>();
            order.Creator = creator;
            order.Assignee = null;
            order.Title = $"[{TestTag}] duplicate check";
            return order;
        }).ToArray();

        await using (var context = TestHost.NewDbContext())
        {
            context.Add(creator);
            context.AddRange(orders.Cast<object>());
            await context.SaveChangesAsync();
        }

        await Click(nameof(NavMenu.Elements.Search));
        var creatorSelect = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        await Expect(creatorSelect.Locator($"option[value='{creator.UserName}']")).ToHaveCountAsync(1);
        await creatorSelect.SelectOptionAsync(creator.UserName);

        foreach (var order in orders)
        {
            await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number))
                .ToHaveCountAsync(1);
        }

        await Expect(Page.Locator(".grid-data tbody tr")).ToHaveCountAsync(orders.Length);
    }
    [Test, Retry(2)]
    public async Task ShouldKeepFiltersChipsAndRows_WhenPageReloaded()
    {
        var creator = await SeedCreatorAsync("reload");
        var overdueHigh = await SeedOrderAsync(creator, WorkOrderPriority.High, WorkOrderStatus.InProgress, new DateOnly(2000, 1, 1));
        await SeedOrderAsync(creator, WorkOrderPriority.High, WorkOrderStatus.InProgress, new DateOnly(2099, 1, 1));
        await SeedOrderAsync(creator, WorkOrderPriority.Low, WorkOrderStatus.InProgress, new DateOnly(2000, 1, 1));

        await OpenSearchAsync();
        await SelectFilterAsync(WorkOrderSearch.Elements.CreatorSelect, creator.UserName);
        await SelectFilterAsync(WorkOrderSearch.Elements.PrioritySelect, nameof(WorkOrderPriority.High));
        await Page.Locator($"#{WorkOrderSearch.Elements.OverdueOnlyToggle}").CheckAsync();
        await Expect(Rows()).ToHaveCountAsync(1);

        await Page.ReloadAsync();

        Page.Url.ShouldContain($"Creator={creator.UserName}");
        Page.Url.ShouldContain("Priority=High");
        Page.Url.ShouldContain("OverdueOnly=true");
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}")).ToHaveValueAsync(creator.UserName);
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.PrioritySelect}")).ToHaveValueAsync(nameof(WorkOrderPriority.High));
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.OverdueOnlyToggle}")).ToBeCheckedAsync();
        await Expect(Chip("Creator")).ToContainTextAsync(creator.GetFullName());
        await Expect(Chip("Priority")).ToContainTextAsync("Priority: High");
        await Expect(Rows()).ToHaveCountAsync(1);
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + overdueHigh.Number)).ToHaveCountAsync(1);
    }

    [Test, Retry(2)]
    public async Task ShouldFilterTable_WhenSummaryCardClicked_AndShowMatchingCount()
    {
        var creator = await SeedCreatorAsync("cards");
        await SeedOrderAsync(creator, WorkOrderPriority.Medium, WorkOrderStatus.Assigned, null, CurrentUser);
        await SeedOrderAsync(creator, WorkOrderPriority.High, WorkOrderStatus.InProgress, null, CurrentUser);
        await SeedOrderAsync(creator, WorkOrderPriority.Low, WorkOrderStatus.Complete, null, CurrentUser);

        await OpenSearchAsync();
        var card = Page.GetByTestId(nameof(WorkOrderSummaryCards.Elements.SummaryCardAssignedToMe));
        await Expect(card.Locator(".summary-card-value")).ToHaveTextAsync("2");

        await card.ClickAsync();
        await Page.WaitForURLAsync(new Regex("AssignedToMe=true&OpenOnly=true"));
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}")).ToBeCheckedAsync();
        await Expect(Chip("OpenOnly")).ToHaveTextAsync("Not closed");
        await Expect(Rows()).ToHaveCountAsync(2);
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.ResultsCountPill))).ToHaveTextAsync("2 of 2");
    }

    [Test, Retry(2)]
    public async Task ShouldRemoveFilter_WhenChipClicked()
    {
        var creator = await SeedCreatorAsync("chips");
        await SeedOrderAsync(creator, WorkOrderPriority.High, WorkOrderStatus.Draft, null);
        await SeedOrderAsync(creator, WorkOrderPriority.Low, WorkOrderStatus.Draft, null);

        await OpenSearchAsync();
        await SelectFilterAsync(WorkOrderSearch.Elements.CreatorSelect, creator.UserName);
        await SelectFilterAsync(WorkOrderSearch.Elements.PrioritySelect, nameof(WorkOrderPriority.High));
        await Expect(Rows()).ToHaveCountAsync(1);

        await Chip("Priority").ClickAsync();

        await Expect(Chip("Priority")).ToHaveCountAsync(0);
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.PrioritySelect}")).ToHaveValueAsync("");
        await Expect(Rows()).ToHaveCountAsync(2);
        Page.Url.ShouldNotContain("Priority=");
    }

    [Test, Retry(2)]
    public async Task ShouldNavigateToSearchWithQ_FromGlobalSearch()
    {
        var creator = await SeedCreatorAsync("global");
        var order = await SeedOrderAsync(creator, WorkOrderPriority.Medium, WorkOrderStatus.Draft, null);

        var search = Page.GetByTestId(nameof(GlobalSearchBox.Elements.GlobalSearchInput));
        await search.FillAsync(order.Number!);
        await search.PressAsync("Enter");
        await Page.WaitForURLAsync($"**/workorder/search?Q={order.Number}");

        await Expect(Chip("Q")).ToHaveTextAsync($"Search: {order.Number}");
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number)).ToHaveCountAsync(1);
    }

    [Test, Retry(2)]
    public async Task ShouldBulkAssignSelectedDrafts()
    {
        var fixer = await SeedCreatorAsync("fixer");
        var first = await SeedOrderAsync(CurrentUser, WorkOrderPriority.Medium, WorkOrderStatus.Draft, null);
        var second = await SeedOrderAsync(CurrentUser, WorkOrderPriority.High, WorkOrderStatus.Draft, null);

        await OpenSearchAsync();
        await SelectFilterAsync(WorkOrderSearch.Elements.CreatorSelect, CurrentUser.UserName);
        await Expect(Rows()).ToHaveCountAsync(2);
        await RowCheckbox(first).CheckAsync();
        await RowCheckbox(second).CheckAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderBulkActionBar.Elements.BulkSelectedCount))).ToHaveTextAsync("2 selected");

        await Page.GetByTestId(nameof(WorkOrderBulkActionBar.Elements.BulkAssignSelect)).SelectOptionAsync(fixer.UserName);
        await Page.GetByTestId(nameof(WorkOrderBulkActionBar.Elements.BulkAssignButton)).ClickAsync();

        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.BulkOutcome))).ToHaveTextAsync("2 updated, 0 skipped");
        foreach (var order in new[] { first, second })
        {
            await Expect(Page.GetByTestId($"{WorkOrderSearch.Elements.StatusCell}{order.Number}").Locator(".status-badge"))
                .ToHaveTextAsync(WorkOrderStatus.Assigned.FriendlyName);
            await Expect(Page.GetByTestId($"{WorkOrderSearch.Elements.AssigneeCell}{order.Number}")).ToContainTextAsync(fixer.GetFullName());
            var saved = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
            saved!.Status.ShouldBe(WorkOrderStatus.Assigned);
            saved.Assignee!.UserName.ShouldBe(fixer.UserName);
        }
    }

    [Test, Retry(2)]
    public async Task ShouldBulkChangeStatus_AndReportSkipped()
    {
        var assignee = await SeedCreatorAsync("worker");
        var assignedOne = await SeedOrderAsync(CurrentUser, WorkOrderPriority.Medium, WorkOrderStatus.Assigned, null, assignee);
        var assignedTwo = await SeedOrderAsync(CurrentUser, WorkOrderPriority.Medium, WorkOrderStatus.Assigned, null, assignee);
        var draft = await SeedOrderAsync(CurrentUser, WorkOrderPriority.Low, WorkOrderStatus.Draft, null);

        await OpenSearchAsync();
        await SelectFilterAsync(WorkOrderSearch.Elements.CreatorSelect, CurrentUser.UserName);
        await Expect(Rows()).ToHaveCountAsync(3);
        await Page.Locator($"#{WorkOrderSearch.Elements.SelectAllCheckbox}").CheckAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderBulkActionBar.Elements.BulkSelectedCount))).ToHaveTextAsync("3 selected");

        await Page.GetByTestId(nameof(WorkOrderBulkActionBar.Elements.BulkChangeStatusSelect)).SelectOptionAsync("Cancel");
        await Page.GetByTestId(nameof(WorkOrderBulkActionBar.Elements.BulkChangeStatusButton)).ClickAsync();

        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.BulkOutcome))).ToHaveTextAsync("2 updated, 1 skipped");
        (await Bus.Send(new WorkOrderByNumberQuery(assignedOne.Number!)))!.Status.ShouldBe(WorkOrderStatus.Cancelled);
        (await Bus.Send(new WorkOrderByNumberQuery(assignedTwo.Number!)))!.Status.ShouldBe(WorkOrderStatus.Cancelled);
        (await Bus.Send(new WorkOrderByNumberQuery(draft.Number!)))!.Status.ShouldBe(WorkOrderStatus.Draft);
    }

    [Test, Retry(2)]
    public async Task ShouldPageThroughResults()
    {
        var creator = await SeedCreatorAsync("pages");
        for (var i = 0; i < 30; i++)
        {
            await SeedOrderAsync(creator, WorkOrderPriority.Medium, WorkOrderStatus.Draft, null);
        }

        await OpenSearchAsync();
        await SelectFilterAsync(WorkOrderSearch.Elements.CreatorSelect, creator.UserName);
        var pill = Page.GetByTestId(nameof(WorkOrderSearch.Elements.ResultsCountPill));
        await Expect(pill).ToHaveTextAsync("25 of 30");
        await Expect(Rows()).ToHaveCountAsync(25);

        await Page.GetByTestId(nameof(WorkOrderSearch.Elements.PageNextButton)).ClickAsync();

        await Expect(pill).ToHaveTextAsync("5 of 30");
        await Expect(Rows()).ToHaveCountAsync(5);
        await Expect(Page.GetByTestId($"{WorkOrderSearch.Elements.PageButton}2")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.ResultsRangeText))).ToContainTextAsync("Showing 26–30 of 30 work orders");
        await Page.WaitForURLAsync(new Regex("Page=2"));

        await Page.GetByTestId(nameof(WorkOrderSearch.Elements.PagePrevButton)).ClickAsync();
        await Expect(pill).ToHaveTextAsync("25 of 30");
    }

    [Test, Retry(2)]
    public async Task ShouldSortByPriority_WhenHeaderClicked()
    {
        var creator = await SeedCreatorAsync("prio");
        await SeedOrderAsync(creator, WorkOrderPriority.Medium, WorkOrderStatus.Draft, null);
        await SeedOrderAsync(creator, WorkOrderPriority.Low, WorkOrderStatus.Draft, null);
        await SeedOrderAsync(creator, WorkOrderPriority.High, WorkOrderStatus.Draft, null);

        await OpenSearchAsync();
        await SelectFilterAsync(WorkOrderSearch.Elements.CreatorSelect, creator.UserName);
        var sort = Page.Locator($"#{WorkOrderSearch.Elements.SortByPriorityButton}");
        var firstPriority = Rows().First.Locator($"[data-testid^='{WorkOrderSearch.Elements.PriorityCell}']");

        await sort.ClickAsync();
        await Expect(sort).ToHaveTextAsync("Priority ▲");
        await Expect(firstPriority).ToHaveTextAsync("Low");
        await Expect(Page.Locator("th[aria-sort='ascending']")).ToHaveCountAsync(1);

        await sort.ClickAsync();
        await Expect(sort).ToHaveTextAsync("Priority ▼");
        await Expect(firstPriority).ToHaveTextAsync("High");
        await Page.WaitForURLAsync(new Regex("Sort=Priority&Dir=desc"));
    }

    [Test, Retry(2)]
    public async Task ShouldShowStatusPillColours()
    {
        var creator = await SeedCreatorAsync("pills");
        var expected = new Dictionary<WorkOrderStatus, (string Color, string Background)>
        {
            [WorkOrderStatus.Assigned] = ("rgb(29, 78, 216)", "rgb(219, 234, 254)"),
            [WorkOrderStatus.InProgress] = ("rgb(180, 83, 9)", "rgb(254, 243, 199)"),
            [WorkOrderStatus.Draft] = ("rgb(71, 85, 105)", "rgb(241, 245, 249)"),
            [WorkOrderStatus.Complete] = ("rgb(4, 120, 87)", "rgb(209, 250, 229)")
        };
        var orders = new Dictionary<WorkOrderStatus, WorkOrder>();
        foreach (var status in expected.Keys)
        {
            orders[status] = await SeedOrderAsync(creator, WorkOrderPriority.Medium, status, null);
        }

        await OpenSearchAsync();
        await SelectFilterAsync(WorkOrderSearch.Elements.CreatorSelect, creator.UserName);

        foreach (var (status, colours) in expected)
        {
            var pill = Page.GetByTestId($"{WorkOrderSearch.Elements.StatusCell}{orders[status].Number}").Locator(".status-badge");
            await Expect(pill).ToHaveTextAsync(status.FriendlyName);
            (await pill.EvaluateAsync<string>("e => getComputedStyle(e).color")).ShouldBe(colours.Color);
            (await pill.EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor")).ShouldBe(colours.Background);
        }
    }

    [Test, Retry(2)]
    public async Task ShouldReachFiltersChipsRowsAndBulkBarByKeyboard()
    {
        var creator = await SeedCreatorAsync("keys");
        var order = await SeedOrderAsync(creator, WorkOrderPriority.High, WorkOrderStatus.Draft, null);

        await OpenSearchAsync();
        await SelectFilterAsync(WorkOrderSearch.Elements.CreatorSelect, creator.UserName);
        await Expect(Rows()).ToHaveCountAsync(1);
        await Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}").FocusAsync();

        await TabUntilFocusedAsync($"[data-testid='{WorkOrderSearch.Elements.ActiveFilterChip}Creator']");
        (await FocusedOutlineWidthAsync()).ShouldBeGreaterThan(0);

        await TabUntilFocusedAsync($"[data-testid='{WorkOrderSearch.Elements.RowCheckbox}{order.Number}']");
        (await FocusedOutlineWidthAsync()).ShouldBeGreaterThan(0);
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.GetByTestId(nameof(WorkOrderBulkActionBar.Elements.BulkActionBar))).ToBeVisibleAsync();

        await TabUntilFocusedAsync($"[data-testid='{WorkOrderBulkActionBar.Elements.BulkAssignSelect}']");
        (await FocusedOutlineWidthAsync()).ShouldBeGreaterThan(0);
    }

    [Test, Retry(2)]
    public async Task ShouldShowControlsAt38PxAnd12PxCardRadius()
    {
        await OpenSearchAsync();

        var select = Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}");
        (await select.EvaluateAsync<double>("e => e.getBoundingClientRect().height")).ShouldBe(38, 0.5);
        (await Page.GetByTestId(nameof(WorkOrderSearch.Elements.ExportButton))
            .EvaluateAsync<double>("e => e.getBoundingClientRect().height")).ShouldBe(38, 0.5);
        (await select.EvaluateAsync<string>("e => getComputedStyle(e).borderTopLeftRadius")).ShouldBe("8px");
        (await Page.Locator(".filters-card").EvaluateAsync<string>("e => getComputedStyle(e).borderTopLeftRadius")).ShouldBe("12px");
        (await Page.Locator(".summary-card").First.EvaluateAsync<string>("e => getComputedStyle(e).borderTopLeftRadius")).ShouldBe("12px");
        (await Page.Locator(".page-heading h1").EvaluateAsync<string>("e => getComputedStyle(e).fontSize")).ShouldBe("26px");
    }

    [Test, Retry(2)]
    public async Task ShouldFocusGlobalSearch_WhenCtrlKPressed()
    {
        await OpenSearchAsync();
        await Page.Locator("main").ClickAsync(new LocatorClickOptions { Position = new Position { X = 5, Y = 5 } });

        await Page.Keyboard.PressAsync("Control+k");

        await Expect(Page.GetByTestId(nameof(GlobalSearchBox.Elements.GlobalSearchInput))).ToBeFocusedAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldShowTableResultsAsBoardCards_WhenMoreThan25Match()
    {
        var creator = await SeedCreatorAsync("board");
        var seeded = new Dictionary<WorkOrderStatus, List<string>>();
        foreach (var status in WorkOrderStatus.GetAllItems())
        {
            seeded[status] = [];
            for (var i = 0; i < 6; i++)
            {
                seeded[status].Add((await SeedOrderAsync(creator, WorkOrderPriority.Medium, status, null)).Number!);
            }
        }

        await OpenSearchAsync();
        await SelectFilterAsync(WorkOrderSearch.Elements.CreatorSelect, creator.UserName);
        var pill = Page.GetByTestId(nameof(WorkOrderSearch.Elements.ResultsCountPill));
        await Expect(pill).ToHaveTextAsync("25 of 30");
        await Expect(Rows()).ToHaveCountAsync(25);
        var tableNumbers = (await TestIdSuffixesAsync(TableLinks(), WorkOrderSearch.Elements.WorkOrderLink)).ToHashSet();
        await Page.GetByTestId(nameof(WorkOrderSearch.Elements.PageNextButton)).ClickAsync();
        await Expect(Rows()).ToHaveCountAsync(5);
        tableNumbers.UnionWith(await TestIdSuffixesAsync(TableLinks(), WorkOrderSearch.Elements.WorkOrderLink));
        tableNumbers.Count.ShouldBe(30);

        var boardButton = Page.GetByTestId(nameof(WorkOrderSearch.Elements.BoardViewButton));
        await boardButton.ClickAsync();

        await Expect(boardButton).ToHaveAttributeAsync("aria-pressed", "true");
        await Page.WaitForURLAsync(new Regex("View=Board"));
        await Expect(pill).ToHaveTextAsync("30 of 30");
        await Expect(Cards(Page.Locator("body"))).ToHaveCountAsync(30);
        (await TestIdSuffixesAsync(Cards(Page.Locator("body")), WorkOrderSearch.Elements.BoardCard)).ToHashSet()
            .SetEquals(tableNumbers).ShouldBeTrue();
        foreach (var (status, numbers) in seeded)
        {
            (await TestIdSuffixesAsync(Cards(BoardColumn(status)), WorkOrderSearch.Elements.BoardCard)).Order()
                .ShouldBe(numbers.Order());
        }

        await Page.ReloadAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.BoardView))).ToBeVisibleAsync();
        await Expect(Cards(Page.Locator("body"))).ToHaveCountAsync(30);

        await SelectFilterAsync(WorkOrderSearch.Elements.StatusSelect, WorkOrderStatus.Draft.Key);
        await Expect(Cards(BoardColumn(WorkOrderStatus.Draft))).ToHaveCountAsync(6);
        await Expect(Page.Locator($"[data-testid^='{WorkOrderSearch.Elements.BoardColumnEmpty}']")).ToHaveCountAsync(4);
        Page.Url.ShouldContain("View=Board");
    }

    [Test, Retry(2)]
    public async Task ShouldReachSwitchAndOpenBoardCardByKeyboard()
    {
        var creator = await SeedCreatorAsync("board_keys");
        var order = await SeedOrderAsync(creator, WorkOrderPriority.High, WorkOrderStatus.Draft, null);

        await OpenSearchAsync();
        await SelectFilterAsync(WorkOrderSearch.Elements.CreatorSelect, creator.UserName);
        await Expect(Rows()).ToHaveCountAsync(1);
        await Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}").FocusAsync();

        var boardButton = $"[data-testid='{WorkOrderSearch.Elements.BoardViewButton}']";
        await TabUntilFocusedAsync(boardButton);
        (await FocusedOutlineWidthAsync()).ShouldBeGreaterThan(0);
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator(boardButton)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Cards(BoardColumn(WorkOrderStatus.Draft))).ToHaveCountAsync(1);

        await TabUntilFocusedAsync($"[data-testid='{WorkOrderSearch.Elements.BoardCard}{order.Number}']");
        (await FocusedOutlineWidthAsync()).ShouldBeGreaterThan(0);
        await Page.Keyboard.PressAsync("Enter");
        await Page.WaitForURLAsync($"**/workorder/manage/{order.Number}?mode=Edit");

        await Page.GoBackAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.BoardView))).ToBeVisibleAsync();
        await Expect(Page.Locator(boardButton)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    private async Task TabUntilFocusedAsync(string selector, int maxTabs = 60)
    {
        for (var i = 0; i < maxTabs; i++)
        {
            if (await Page.EvaluateAsync<bool>("s => document.activeElement?.matches(s) === true", selector))
            {
                return;
            }

            await Page.Keyboard.PressAsync("Tab");
        }

        Assert.Fail($"{selector} was not reached within {maxTabs} Tab presses");
    }

    private async Task<double> FocusedOutlineWidthAsync() =>
        await Page.EvaluateAsync<double>("() => parseFloat(getComputedStyle(document.activeElement).outlineWidth) || 0");

    private ILocator RowCheckbox(WorkOrder order) =>
        Page.GetByTestId($"{WorkOrderSearch.Elements.RowCheckbox}{order.Number}");

    private ILocator Rows() => Page.Locator(".grid-data tbody tr");

    private ILocator TableLinks() => Page.Locator($".grid-data [data-testid^='{WorkOrderSearch.Elements.WorkOrderLink}']");

    private ILocator BoardColumn(WorkOrderStatus status) =>
        Page.GetByTestId($"{WorkOrderSearch.Elements.BoardColumn}{status.Key}");

    private static ILocator Cards(ILocator container) =>
        container.Locator($"[data-testid^='{WorkOrderSearch.Elements.BoardCard}']");

    private static async Task<string[]> TestIdSuffixesAsync(ILocator elements, WorkOrderSearch.Elements prefix) =>
        (await elements.EvaluateAllAsync<string[]>("els => els.map(e => e.getAttribute('data-testid'))"))
        .Select(testId => testId[prefix.ToString().Length..])
        .ToArray();

    private ILocator Chip(string key) => Page.GetByTestId($"{WorkOrderSearch.Elements.ActiveFilterChip}{key}");

    private async Task OpenSearchAsync()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search**");
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}")).ToBeVisibleAsync();
    }

    private async Task SelectFilterAsync(WorkOrderSearch.Elements select, string value)
    {
        var locator = Page.Locator($"#{select}");
        await Expect(locator.Locator($"option[value='{value}']")).ToHaveCountAsync(1);
        await locator.SelectOptionAsync(value);
        await Expect(locator).ToHaveValueAsync(value);
    }

    private async Task<Employee> SeedCreatorAsync(string prefix)
    {
        var creator = Faker<Employee>();
        creator.UserName = $"{prefix}_{TestTag}";
        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        await context.SaveChangesAsync();
        return creator;
    }

    private async Task<WorkOrder> SeedOrderAsync(Employee creator, WorkOrderPriority priority, WorkOrderStatus status,
        DateOnly? dueDate, Employee? assignee = null)
    {
        var order = Faker<WorkOrder>();
        order.Creator = creator;
        order.Assignee = assignee;
        order.Priority = priority;
        order.Status = status;
        order.DueDate = dueDate;
        order.CompletedDate = null;
        order.Title = $"[{TestTag}] {priority} {status.FriendlyName}";
        await using var context = TestHost.NewDbContext();
        context.Attach(creator);
        if (assignee != null && assignee != creator)
        {
            context.Attach(assignee);
        }

        context.Add(order);
        await context.SaveChangesAsync();
        return order;
    }
}
