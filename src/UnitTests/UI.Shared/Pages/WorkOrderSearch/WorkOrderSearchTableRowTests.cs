using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.AspNetCore.Components.Web;
using Shouldly;
using WorkOrderSearchPage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

public class WorkOrderSearchTableRowTests
{
    [Test]
    public async Task ShouldApply_OverdueRow_CssClass_WhenWorkOrderIsOverdue()
    {
        var overdueOrder = WorkOrderBuilder.Create().WithNumber("WO-OVR").WithTitle("Overdue")
            .WithStatus(WorkOrderStatus.InProgress).DueOn(new DateOnly(2000, 1, 1)).Build();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(overdueOrder).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        var rows = component.FindAll("tbody tr");
        rows.Count.ShouldBe(1);
        rows[0].ClassName!.ShouldContain("overdue-row");
    }

    [Test]
    public async Task ShouldNotApply_OverdueRow_CssClass_WhenWorkOrderIsNotOverdue()
    {
        var nonOverdueOrder = WorkOrderBuilder.Create().WithNumber("WO-FUT").WithTitle("Future")
            .WithStatus(WorkOrderStatus.InProgress).DueOn(new DateOnly(2099, 12, 31)).Build();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(nonOverdueOrder).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        var rows = component.FindAll("tbody tr");
        rows.Count.ShouldBe(1);
        rows[0].ClassName!.ShouldNotContain("overdue-row");
    }

    [Test]
    public async Task ShouldRenderColumnsInMockupOrder()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        component.FindAll("thead th").Select(th => th.TextContent.Trim())
            .ShouldBe(["", "Work Order", "Title", "Status", "Priority", "Assignee", "Room", "Due Date", "Actions"]);
    }

    [Test]
    public async Task ShouldShowDashAndHiddenText_WhenNoDueDate()
    {
        var order = WorkOrderBuilder.Create().WithNumber("WO-ND").WithTitle("No date")
            .WithStatus(WorkOrderStatus.Draft).DueOn(null).Build();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(order).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        var cell = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.DueDateCell, "WO-ND"));
        cell.TextContent.ShouldBe("–");
        cell.ParentElement!.QuerySelector(".visually-hidden")!.TextContent.ShouldBe("No due date");
    }

    [Test]
    public async Task ShouldShowUnassigned_WhenNoAssignee()
    {
        var order = WorkOrderBuilder.Create().WithNumber("WO-UA").WithTitle("Nobody")
            .WithStatus(WorkOrderStatus.Draft).Build();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(order).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        var cell = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.AssigneeCell, "WO-UA"));
        cell.TextContent.Trim().ShouldBe("Unassigned");
        cell.QuerySelector(".avatar--empty").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldShowAssigneeInitialsAndPriorityLabel()
    {
        var assignee = EmployeeBuilder.Create("gwillie").WithFirstName("Groundskeeper").WithLastName("Willie")
            .WithEmail("g@x").Build();
        var order = WorkOrderBuilder.Create().WithNumber("WO-AS").WithTitle("Organ")
            .WithStatus(WorkOrderStatus.Assigned).WithPriority(WorkOrderPriority.High).AssignedTo(assignee).Build();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(order).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        component.Find($"{WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.AssigneeCell, "WO-AS")} .avatar")
            .TextContent.ShouldBe("GW");
        var priority = component.Find($"{WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.PriorityCell, "WO-AS")} .priority");
        priority.ClassList.ShouldContain("priority--high");
        priority.TextContent.Trim().ShouldBe("High");
        component.Find($"{WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.StatusCell, "WO-AS")} .status-badge")
            .TextContent.ShouldBe("Assigned");
    }

    [Test]
    public async Task ShouldShowPriorityBadge_WhenHighAndFlagOn()
    {
        var order = WorkOrderBuilder.Create().WithNumber("WO-HI").WithTitle("Roof")
            .WithStatus(WorkOrderStatus.Assigned).WithPriority(WorkOrderPriority.High).Build();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(order).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        var badge = component.Find("[data-testid='PriorityBadgeWO-HI']");
        badge.ClassList.ShouldContain("priority-badge");
        badge.ClassList.ShouldContain("priority-badge--high");
        badge.TextContent.ShouldBe("High");
        component.Find($"{WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.PriorityCell, "WO-HI")} .priority")
            .TextContent.Trim().ShouldBe("High");
    }

    [Test]
    public async Task ShouldHidePriorityBadge_WhenHighAndFlagOff()
    {
        var order = WorkOrderBuilder.Create().WithNumber("WO-HI").WithTitle("Roof")
            .WithStatus(WorkOrderStatus.Assigned).WithPriority(WorkOrderPriority.High).Build();
        var flags = new StubFeatureFlags(showPriorityBadges: false);
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(order).WithFeatureFlags(flags).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        component.FindAll(".priority-badge").Count.ShouldBe(0);
        component.Find($"{WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.PriorityCell, "WO-HI")} .priority")
            .TextContent.Trim().ShouldBe("High");
    }

    [Test]
    public async Task ShouldNotShowPriorityBadge_WhenLowOrMediumAndFlagOn()
    {
        var low = WorkOrderBuilder.Create().WithNumber("WO-LO").WithTitle("Low")
            .WithStatus(WorkOrderStatus.Draft).WithPriority(WorkOrderPriority.Low).Build();
        var medium = WorkOrderBuilder.Create().WithNumber("WO-MD").WithTitle("Med")
            .WithStatus(WorkOrderStatus.Draft).WithPriority(WorkOrderPriority.Medium).Build();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(low, medium).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        component.FindAll(".priority-badge").Count.ShouldBe(0);
        component.Find($"{WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.PriorityCell, "WO-LO")} .priority")
            .TextContent.Trim().ShouldBe("Low");
        component.Find($"{WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.PriorityCell, "WO-MD")} .priority")
            .TextContent.Trim().ShouldBe("Medium");
    }

    [Test]
    public async Task ShouldPrefixDisplayNumberButKeepHrefAndTestId()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        var link = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.WorkOrderLink, "WO-001"));
        link.TextContent.ShouldBe("WO-WO-001");
        link.GetAttribute("href").ShouldBe("/workorder/manage/WO-001?mode=Edit");
    }

    [Test]
    public async Task ShouldShowOverduePillWithHiddenStoredStatus()
    {
        var order = WorkOrderBuilder.Create().WithNumber("WO-OD").WithTitle("Late")
            .WithStatus(WorkOrderStatus.InProgress).DueOn(new DateOnly(2000, 1, 1)).Build();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(order).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        var cell = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.StatusCell, "WO-OD"));
        cell.QuerySelector(".status-badge")!.TextContent.ShouldBe("Overdue");
        cell.QuerySelector(".status-badge")!.ClassList.ShouldContain("status-badge--overdue");
        cell.QuerySelector(".visually-hidden")!.TextContent.ShouldBe(", status In Progress");
    }

    [Test]
    public async Task RowMenu_OpensWithOpenLink()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.ClickTestIdAsync(component, WorkOrderSearchPage.Elements.RowMenu, "WO-001");

        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.RowMenuOpen, "WO-001"))
            .GetAttribute("href").ShouldBe("/workorder/manage/WO-001?mode=Edit");
        await component.Find(".col-actions .menu-anchor").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.RowMenuOpen, "WO-001")).Count.ShouldBe(0);
    }
}
