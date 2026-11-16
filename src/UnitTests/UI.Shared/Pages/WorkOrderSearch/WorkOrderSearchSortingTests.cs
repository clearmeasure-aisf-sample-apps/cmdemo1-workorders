using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;
using WorkOrderSearchPage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

public class WorkOrderSearchSortingTests
{
    [Test]
    public async Task SortByStatus_Ascending_SortsResultsByStatusFriendlyName()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(WorkOrderSearchSteps.OrdersForStatusSort()).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.ClickAsync(component, $"#{WorkOrderSearchPage.Elements.SortByStatusButton}");

        var cells = component.FindAll($"[data-testid^='{WorkOrderSearchPage.Elements.StatusCell}'] .status-badge");
        cells[0].TextContent.Trim().ShouldBe(WorkOrderStatus.Assigned.FriendlyName);
        cells[1].TextContent.Trim().ShouldBe(WorkOrderStatus.Draft.FriendlyName);
        cells[2].TextContent.Trim().ShouldBe(WorkOrderStatus.InProgress.FriendlyName);
    }

    [Test]
    public async Task SortByStatus_ClickingAgain_ReversesToDescending()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(WorkOrderSearchSteps.OrdersForStatusSort()).Build();
        var component = ctx.Render<WorkOrderSearchPage>();
        var sortBtn = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.SortByStatusButton);

        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll($"[data-testid^='{WorkOrderSearchPage.Elements.StatusCell}'] .status-badge");
        cells[0].TextContent.Trim().ShouldBe(WorkOrderStatus.InProgress.FriendlyName);
        cells[1].TextContent.Trim().ShouldBe(WorkOrderStatus.Draft.FriendlyName);
        cells[2].TextContent.Trim().ShouldBe(WorkOrderStatus.Assigned.FriendlyName);
    }

    [Test]
    public async Task SortByDueDate_Ascending_SortsResultsByDueDate_NullsLast()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(WorkOrderSearchSteps.OrdersForDueDateSort()).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.ClickAsync(component, $"#{WorkOrderSearchPage.Elements.SortByDueDateButton}");

        WorkOrderSearchSteps.LinkNumbers(component).ShouldBe(["WO-002", "WO-001", "WO-003"]);
    }

    [Test]
    public async Task SortByDueDate_ClickingAgain_ReversesToDescending_NullsLast()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(WorkOrderSearchSteps.OrdersForDueDateSort()).Build();
        var component = ctx.Render<WorkOrderSearchPage>();
        var sortBtn = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.SortByDueDateButton);

        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        WorkOrderSearchSteps.LinkNumbers(component).ShouldBe(["WO-001", "WO-002", "WO-003"]);
    }

    [Test]
    public async Task SortByDifferentColumn_ResetsDirectionToAscending()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create()
            .WithOrders(WorkOrderSearchSteps.OrdersForColumnSwitchSort()).Build();
        var component = ctx.Render<WorkOrderSearchPage>();
        var statusBtn = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.SortByStatusButton);

        await statusBtn.ClickAsync(new());
        await statusBtn.ClickAsync(new());
        await WorkOrderSearchSteps.ClickAsync(component, $"#{WorkOrderSearchPage.Elements.SortByDueDateButton}");

        WorkOrderSearchSteps.LinkNumbers(component).ShouldBe(["WO-001", "WO-003", "WO-002"]);
    }

    [Test]
    public async Task SortIndicatorGlyph_ShowsAscendingGlyph_WhenColumnFirstClicked()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();
        var sortBtn = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.SortByStatusButton);

        await sortBtn.ClickAsync(new());

        sortBtn.TextContent.ShouldContain("▲");
        sortBtn.TextContent.ShouldNotContain("▼");
    }

    [Test]
    public async Task SortIndicatorGlyph_ShowsDescendingGlyph_WhenSameColumnClickedAgain()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();
        var sortBtn = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.SortByStatusButton);

        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        sortBtn.TextContent.ShouldContain("▼");
        sortBtn.TextContent.ShouldNotContain("▲");
    }

    [Test]
    public async Task SortByTitle_Ascending_SortsResultsByTitle()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(WorkOrderSearchSteps.DraftsTitledAbc()).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.ClickAsync(component, $"#{WorkOrderSearchPage.Elements.SortByTitleButton}");

        var cells = component.FindAll($"[data-testid^='{WorkOrderSearchPage.Elements.TitleCell}']");
        cells[0].TextContent.Trim().ShouldBe("A");
        cells[1].TextContent.Trim().ShouldBe("B");
        cells[2].TextContent.Trim().ShouldBe("C");
    }

    [Test]
    public async Task SortByTitle_ClickingAgain_ReversesToDescending()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(WorkOrderSearchSteps.DraftsTitledAbc()).Build();
        var component = ctx.Render<WorkOrderSearchPage>();
        var sortBtn = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.SortByTitleButton);

        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll($"[data-testid^='{WorkOrderSearchPage.Elements.TitleCell}']");
        cells[0].TextContent.Trim().ShouldBe("C");
        cells[1].TextContent.Trim().ShouldBe("B");
        cells[2].TextContent.Trim().ShouldBe("A");
    }

    [Test]
    public async Task SortByRoom_Ascending_SortsResultsByRoomNumber()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(WorkOrderSearchSteps.OrdersForRoomSort()).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.ClickAsync(component, $"#{WorkOrderSearchPage.Elements.SortByRoomButton}");

        var cells = component.FindAll($"[data-testid^='{WorkOrderSearchPage.Elements.RoomCell}']");
        cells[0].TextContent.Trim().ShouldBe("A");
        cells[1].TextContent.Trim().ShouldBe("B");
        cells[2].TextContent.Trim().ShouldBe("C");
    }

    [Test]
    public async Task SortByRoom_ClickingAgain_ReversesToDescending()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(WorkOrderSearchSteps.OrdersForRoomSort()).Build();
        var component = ctx.Render<WorkOrderSearchPage>();
        var sortBtn = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.SortByRoomButton);

        await sortBtn.ClickAsync(new());
        await sortBtn.ClickAsync(new());

        var cells = component.FindAll($"[data-testid^='{WorkOrderSearchPage.Elements.RoomCell}']");
        cells[0].TextContent.Trim().ShouldBe("C");
        cells[1].TextContent.Trim().ShouldBe("B");
        cells[2].TextContent.Trim().ShouldBe("A");
    }

    [Test]
    public async Task SortHeader_SetsAriaSort()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();
        PrioritySortHeader(component).GetAttribute("aria-sort").ShouldBe("none");

        await WorkOrderSearchSteps.ClickAsync(component, $"#{WorkOrderSearchPage.Elements.SortByPriorityButton}");
        PrioritySortHeader(component).GetAttribute("aria-sort").ShouldBe("ascending");

        await WorkOrderSearchSteps.ClickAsync(component, $"#{WorkOrderSearchPage.Elements.SortByPriorityButton}");
        PrioritySortHeader(component).GetAttribute("aria-sort").ShouldBe("descending");
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.SortSelect)
            .QuerySelector("option[selected]")!.TextContent.ShouldBe("Sort: Priority ↓");
    }

    private static AngleSharp.Dom.IElement PrioritySortHeader(IRenderedComponent<WorkOrderSearchPage> component) =>
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.SortByPriorityButton).ParentElement!;

    [Test]
    public async Task SortSelect_SortsLikeTheHeader()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create()
            .WithOrders(WorkOrderSearchSteps.OrdersForPrioritySort()).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.SortSelect, "Priority:desc");
        WorkOrderSearchSteps.LinkNumbers(component).ShouldBe(["WO-H", "WO-M", "WO-L"]);
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.SortByPriorityButton).TextContent
            .ShouldContain("▼");

        await WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.SortSelect, "");
        WorkOrderSearchSteps.LinkNumbers(component).ShouldBe(["WO-L", "WO-H", "WO-M"]);
    }
}
