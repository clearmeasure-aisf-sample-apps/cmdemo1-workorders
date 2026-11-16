using Bunit;
using Bunit.TestDoubles;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WorkOrderSearchPage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

public class WorkOrderSearchBoardTests
{
    [Test]
    public async Task ViewSwitch_BareUrl_TablePressedBoardNotDisabled()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        var table = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.TableViewButton));
        var board = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardViewButton));
        table.GetAttribute("aria-pressed").ShouldBe("true");
        table.ClassList.ShouldContain("is-active");
        board.GetAttribute("aria-pressed").ShouldBe("false");
        board.ClassList.ShouldNotContain("is-active");
        board.HasAttribute("aria-disabled").ShouldBeFalse();
        board.HasAttribute("title").ShouldBeFalse();
        component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardView)).Count.ShouldBe(0);
    }

    [Test]
    public async Task ClickBoard_ShowsAllResultsAsCards_WithoutPagerOrTable()
    {
        var bus = new StubBus(WorkOrderBuilder.ManyDrafts(30));
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<WorkOrderSearchPage>();
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsCountPill)).TextContent
            .ShouldBe("25 of 30");
        var sendsBefore = bus.SendCallCount;

        await WorkOrderSearchSteps.ClickTestIdAsync(component, WorkOrderSearchPage.Elements.BoardViewButton);

        AssertBoardShowsAllCards(component, navigation, bus, sendsBefore);
    }

    private static void AssertBoardShowsAllCards(
        IRenderedComponent<WorkOrderSearchPage> component,
        NavigationManager navigation,
        StubBus bus,
        int sendsBefore)
    {
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardViewButton))
            .GetAttribute("aria-pressed").ShouldBe("true");
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.TableViewButton))
            .GetAttribute("aria-pressed").ShouldBe("false");
        component.FindAll(".grid-data").Count.ShouldBe(0);
        WorkOrderSearchSteps.BoardCardNumbers(component).Length.ShouldBe(30);
        component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.PageNextButton)).Count.ShouldBe(0);
        component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsRangeText)).Count.ShouldBe(0);
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsCountPill)).TextContent
            .ShouldBe("30 of 30");
        navigation.Uri.ShouldEndWith("/workorder/search?View=Board");
        ((BunitNavigationManager)navigation).History.First().Options.ReplaceHistoryEntry.ShouldBeTrue();
        bus.SendCallCount.ShouldBe(sendsBefore);
    }

    [Test]
    public async Task ClickTable_FromBoard_RestoresBareUrlAndTable()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create()
            .WithOrders(WorkOrderBuilder.ManyDrafts(30)).Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("workorder/search?View=Board&Page=2");
        var component = ctx.Render<WorkOrderSearchPage>();
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardView)).ShouldNotBeNull();

        await WorkOrderSearchSteps.ClickTestIdAsync(component, WorkOrderSearchPage.Elements.TableViewButton);

        AssertTableRestored(component, navigation);
    }

    private static void AssertTableRestored(
        IRenderedComponent<WorkOrderSearchPage> component,
        NavigationManager navigation)
    {
        navigation.Uri.ShouldEndWith("/workorder/search");
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.TableViewButton))
            .GetAttribute("aria-pressed").ShouldBe("true");
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardViewButton))
            .GetAttribute("aria-pressed").ShouldBe("false");
        component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardView)).Count.ShouldBe(0);
        component.FindAll("tbody tr").Count.ShouldBe(25);
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsCountPill)).TextContent
            .ShouldBe("25 of 30");
    }

    [Test]
    public async Task Board_WaitingOnVendorCard_ShowsWaitingPillInInProgressColumn()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create()
            .WithOrders(WorkOrderSearchSteps.WaitingAndWorkingInProgress()).Build();
        WorkOrderSearchSteps.NavigateTo(ctx, "workorder/search?View=Board");
        var component = ctx.Render<WorkOrderSearchPage>();

        var column = component.Find(
            WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardColumn, WorkOrderStatus.InProgress.Key));
        var waitingCard = column.QuerySelector(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardCard, "WV001"));
        waitingCard.ShouldNotBeNull();
        waitingCard.QuerySelector(".status-badge--waiting")!.TextContent.ShouldBe("Waiting on vendor");
        column.QuerySelector(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardCard, "WV002"))!
            .QuerySelector(".status-badge--waiting").ShouldBeNull();
    }

    [Test]
    public async Task ClickActiveView_KeepsUrlUnchanged()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var navigation = (BunitNavigationManager)ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<WorkOrderSearchPage>();
        var historyBefore = navigation.History.Count;

        await WorkOrderSearchSteps.ClickTestIdAsync(component, WorkOrderSearchPage.Elements.TableViewButton);

        navigation.History.Count.ShouldBe(historyBefore);
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.TableViewButton))
            .GetAttribute("aria-pressed").ShouldBe("true");
    }

    [Test]
    public async Task BoardUrl_WithCreator_LoadsBoardAndFilters()
    {
        var bus = new StubBus();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        WorkOrderSearchSteps.NavigateTo(ctx, "workorder/search?View=Board&Creator=tlovejoy");

        var component = ctx.Render<WorkOrderSearchPage>();

        bus.LastSpecification!.Creator.ShouldNotBeNull();
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardView)).ShouldNotBeNull();
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardViewButton))
            .GetAttribute("aria-pressed").ShouldBe("true");
        WorkOrderSearchSteps.ChipKeys(component).ShouldBe(["Creator"]);
    }

    [Test]
    public async Task BoardCard_HrefMatchesTableLink()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<WorkOrderSearchPage>();
        var tableHref = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.WorkOrderLink, "WO-001"))
            .GetAttribute("href");

        navigation.NavigateTo("workorder/search?View=Board");

        var card = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardCard, "WO-001"));
        card.GetAttribute("href").ShouldBe(tableHref);
        card.GetAttribute("href").ShouldBe("/workorder/manage/WO-001?mode=Edit");
        card.TextContent.ShouldContain("WO-WO-001");
    }

    [Test]
    public async Task Board_OnlyDraftRows_ShowsFiveColumnsFourEmpty()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create()
            .WithOrders(WorkOrderBuilder.ManyDrafts(30)).Build();
        WorkOrderSearchSteps.NavigateTo(ctx, "workorder/search?View=Board");
        var component = ctx.Render<WorkOrderSearchPage>();

        component.FindAll(".board-column").Count.ShouldBe(5);
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardColumnCount, "Draft"))
            .TextContent.ShouldBe("30");
        component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardColumnEmpty, "Draft"))
            .Count.ShouldBe(0);
        AssertEmptyBoardColumn(component, "Assigned");
        AssertEmptyBoardColumn(component, "InProgress");
        AssertEmptyBoardColumn(component, "Complete");
        AssertEmptyBoardColumn(component, "Cancelled");
    }

    private static void AssertEmptyBoardColumn(IRenderedComponent<WorkOrderSearchPage> component, string key)
    {
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardColumnEmpty, key)).TextContent
            .ShouldBe("No work orders");
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardColumnCount, key)).TextContent
            .ShouldBe("0");
    }

    [Test]
    public async Task Board_OrdersPerStatus_EachColumnHoldsItsStatus()
    {
        var orders = WorkOrderBuilder.OnePerStatus(2);
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(orders).Build();
        WorkOrderSearchSteps.NavigateTo(ctx, "workorder/search?View=Board");
        var component = ctx.Render<WorkOrderSearchPage>();

        AssertColumnHoldsStatus(component, WorkOrderStatus.Draft.Key, "WO-001", "WO-006");
        AssertColumnHoldsStatus(component, WorkOrderStatus.Assigned.Key, "WO-002", "WO-007");
        AssertColumnHoldsStatus(component, WorkOrderStatus.InProgress.Key, "WO-003", "WO-008");
        AssertColumnHoldsStatus(component, WorkOrderStatus.Complete.Key, "WO-004", "WO-009");
        AssertColumnHoldsStatus(component, WorkOrderStatus.Cancelled.Key, "WO-005", "WO-010");
    }

    private static void AssertColumnHoldsStatus(
        IRenderedComponent<WorkOrderSearchPage> component,
        string statusKey,
        string firstNumber,
        string secondNumber)
    {
        var status = WorkOrderStatus.FromKey(statusKey);
        var column = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardColumn, statusKey));
        var heading = column.QuerySelector("h3")!;
        column.GetAttribute("aria-labelledby").ShouldBe(heading.Id);
        heading.QuerySelector(".status-badge")!.TextContent.ShouldBe(status.FriendlyName);
        WorkOrderSearchSteps.CardNumbers(column).ShouldBe([firstNumber, secondNumber]);
    }

    [Test]
    public async Task Board_Card_ShowsTitlePriorityAssigneeRoomAndDueDate()
    {
        var orders = WorkOrderBuilder.OnePerStatus(1);
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(orders).Build();
        WorkOrderSearchSteps.NavigateTo(ctx, "workorder/search?View=Board");
        var component = ctx.Render<WorkOrderSearchPage>();

        AssertAssignedBoardCard(component, orders[1]);
        AssertDraftBoardCard(component);
    }

    private static void AssertAssignedBoardCard(IRenderedComponent<WorkOrderSearchPage> component, WorkOrder assignedOrder)
    {
        var assigned = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardCard, "WO-002"));
        assigned.QuerySelector(".board-card-title")!.TextContent.ShouldBe(assignedOrder.Title);
        assigned.QuerySelector(".priority")!.TextContent.Trim().ShouldBe("High");
        assigned.QuerySelector(".avatar")!.TextContent.ShouldBe("GW");
        assigned.QuerySelector(".board-card-assignee .visually-hidden")!.TextContent
            .ShouldBe("Assigned to Groundskeeper Willie");
        assigned.QuerySelector(".board-card-room")!.TextContent.ShouldBe("101");
        assigned.QuerySelector(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.UrgencyBadge, "WO-002"))!
            .TextContent.ShouldBe("Overdue");
    }

    private static void AssertDraftBoardCard(IRenderedComponent<WorkOrderSearchPage> component)
    {
        var draft = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardCard, "WO-001"));
        draft.QuerySelector(".board-card-assignee .visually-hidden")!.TextContent.ShouldBe("Unassigned");
        draft.QuerySelector(".avatar")!.ClassList.ShouldContain("avatar--empty");
        draft.QuerySelector(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.DueDateCell, "WO-001"))!
            .TextContent.ShouldBe("–");
    }

    [Test]
    public async Task Board_HighCard_ShowsPriorityBadge_WhenFlagOn()
    {
        var order = WorkOrderBuilder.Create().WithNumber("WO-HI").WithTitle("Roof")
            .WithStatus(WorkOrderStatus.Assigned).WithPriority(WorkOrderPriority.High).Build();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(order).Build();
        WorkOrderSearchSteps.NavigateTo(ctx, "workorder/search?View=Board");
        var component = ctx.Render<WorkOrderSearchPage>();

        var card = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardCard, "WO-HI"));
        var badge = card.QuerySelector("[data-testid='PriorityBadgeWO-HI']");
        badge.ShouldNotBeNull();
        badge.ClassList.ShouldContain("priority-badge--high");
        badge.TextContent.ShouldBe("High");
        card.QuerySelector(".priority")!.TextContent.Trim().ShouldBe("High");
    }

    [Test]
    public async Task Board_SortSelectTitleAsc_OrdersCardsInColumn()
    {
        var orders = WorkOrderBuilder.OnePerStatus(3);
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(orders).Build();
        WorkOrderSearchSteps.NavigateTo(ctx, "workorder/search?View=Board");
        var component = ctx.Render<WorkOrderSearchPage>();
        var expectedUnsorted = DraftNumbersInCatalogOrder(orders);
        var expectedSorted = DraftNumbersSortedByTitle(orders);
        WorkOrderSearchSteps.DraftColumnNumbers(component).ShouldBe(expectedUnsorted);

        await WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.SortSelect, "Title:asc");

        WorkOrderSearchSteps.DraftColumnNumbers(component).ShouldBe(expectedSorted);
        WorkOrderSearchSteps.DraftColumnNumbers(component).ShouldNotBe(expectedUnsorted);
        ctx.Services.GetRequiredService<NavigationManager>().Uri
            .ShouldEndWith("/workorder/search?Sort=Title&View=Board");
    }

    private static string[] DraftNumbersInCatalogOrder(WorkOrder[] orders) =>
        orders.Where(o => o.Status == WorkOrderStatus.Draft).Select(o => o.Number!).ToArray();

    private static string[] DraftNumbersSortedByTitle(WorkOrder[] orders) =>
        orders.Where(o => o.Status == WorkOrderStatus.Draft)
            .OrderBy(o => o.Title, StringComparer.OrdinalIgnoreCase)
            .Select(o => o.Number!)
            .ToArray();

    [Test]
    public async Task Board_ClearAllAndSummaryCard_KeepViewBoard()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("workorder/search?View=Board&Creator=tlovejoy");
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.ClickAsync(component, $"#{WorkOrderSearchPage.Elements.ClearFiltersButton}");
        navigation.Uri.ShouldEndWith("/workorder/search?View=Board");
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardView)).ShouldNotBeNull();

        await component.Find("[data-testid='SummaryCardOverdue']").ClickAsync(new());
        navigation.Uri.ShouldEndWith("/workorder/search?OverdueOnly=true&View=Board");
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardViewButton))
            .GetAttribute("aria-pressed").ShouldBe("true");
    }

    [Test]
    public async Task Board_SwitchClearsSelection_NoBulkBar()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();
        await component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.RowCheckbox, "WO-001"))
            .ChangeAsync(new() { Value = true });
        component.FindAll("[data-testid='BulkActionBar']").Count.ShouldBe(1);

        await WorkOrderSearchSteps.ClickTestIdAsync(component, WorkOrderSearchPage.Elements.BoardViewButton);
        component.FindAll("[data-testid='BulkActionBar']").Count.ShouldBe(0);
        component.FindAll("input[type='checkbox'].row-checkbox").Count.ShouldBe(0);

        await WorkOrderSearchSteps.ClickTestIdAsync(component, WorkOrderSearchPage.Elements.TableViewButton);
        component.FindAll("[data-testid='BulkActionBar']").Count.ShouldBe(0);
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsRangeText)).TextContent
            .ShouldEndWith("0 selected");
    }

    [Test]
    public async Task Board_ZeroResults_ShowsEmptyMessage()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(new StubBus([])).Build();
        WorkOrderSearchSteps.NavigateTo(ctx, "workorder/search?View=Board");

        var component = ctx.Render<WorkOrderSearchPage>();

        component.Find(".empty-results").TextContent.ShouldContain("No work orders found");
        component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BoardView)).Count.ShouldBe(0);
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsCountPill)).TextContent
            .ShouldBe("0 of 0");
    }
}
