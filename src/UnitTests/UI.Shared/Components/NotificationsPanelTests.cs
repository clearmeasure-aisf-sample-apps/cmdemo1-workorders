using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.AspNetCore.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class NotificationsPanelTests
{
    private static void SetupJsInterop(BunitContext ctx)
    {
        var module = ctx.JSInterop.SetupModule("./_content/ClearMeasure.Bootcamp.UI.Shared/js/notificationsPanel.js");
        module.SetupVoid("setupEscapeHandler", _ => true);
    }

    [Test]
    public void ShouldNotRenderPanel_WhenIsOpenIsFalse()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, false)
            .Add(c => c.Data, new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel([], [], [])));

        component.Markup.ShouldNotContain("notifications-panel");
    }

    [Test]
    public void ShouldRenderPanel_WhenIsOpenIsTrue()
    {
        using var ctx = new BunitContext();
        SetupJsInterop(ctx);

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel([], [], [])));

        component.Markup.ShouldContain("class=\"notifications-panel\"");
    }

    [Test]
    public void ShouldRenderAllThreeSections_WhenOpen()
    {
        using var ctx = new BunitContext();
        SetupJsInterop(ctx);

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel([], [], [])));

        component.Markup.ShouldContain("Overdue & Due Today");
        component.Markup.ShouldContain("Newly Assigned (Last 7 Days)");
        component.Markup.ShouldContain("Status Changed (Last 7 Days)");
    }

    [Test]
    public void ShouldRenderEmptyMessage_WhenOverdueAndDueTodayIsEmpty()
    {
        using var ctx = new BunitContext();
        SetupJsInterop(ctx);

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel([], [], [])));

        component.Markup.ShouldContain("No overdue or due-today work orders");
    }

    [Test]
    public void ShouldRenderWorkOrderCards_WhenOverdueAndDueTodayHasItems()
    {
        using var ctx = new BunitContext();
        SetupJsInterop(ctx);

        var creator = new Employee("test_user", "Test", "User", "test@example.com");
        var assignee = new Employee("assigned_user", "Assigned", "User", "assigned@example.com");
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-001",
            Title = "Test Order",
            Creator = creator,
            Assignee = assignee,
            Status = WorkOrderStatus.Assigned,
            DueDate = DateOnly.FromDateTime(DateTime.Today)
        };

        var data = new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel(
            [workOrder],
            [],
            []);

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, data));

        component.Markup.ShouldContain("WO-001");
        component.Markup.ShouldContain("Test Order");
        component.Markup.ShouldContain("Assigned User");
    }

    [Test]
    public void ShouldRenderWorkOrderAsLink_ToManageRoute()
    {
        using var ctx = new BunitContext();
        SetupJsInterop(ctx);

        var creator = new Employee("test_user", "Test", "User", "test@example.com");
        var assignee = new Employee("assigned_user", "Assigned", "User", "assigned@example.com");
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-001",
            Title = "Test Order",
            Creator = creator,
            Assignee = assignee,
            Status = WorkOrderStatus.Assigned,
            DueDate = DateOnly.FromDateTime(DateTime.Today)
        };

        var data = new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel(
            [workOrder],
            [],
            []);

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, data));

        var link = component.Find("a.notification-card");
        link.GetAttribute("href").ShouldBe("/workorder/manage/WO-001?mode=Edit");
    }

    [Test]
    public void ShouldRenderNewlyAssignedWorkOrders()
    {
        using var ctx = new BunitContext();
        SetupJsInterop(ctx);

        var creator = new Employee("test_user", "Test", "User", "test@example.com");
        var assignee = new Employee("assigned_user", "Assigned", "User", "assigned@example.com");
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-002",
            Title = "New Assignment",
            Creator = creator,
            Assignee = assignee,
            Status = WorkOrderStatus.Assigned,
            AssignedDate = DateTime.UtcNow.AddDays(-2)
        };

        var data = new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel(
            [],
            [workOrder],
            []);

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, data));

        component.Markup.ShouldContain("WO-002");
        component.Markup.ShouldContain("New Assignment");
    }

    [Test]
    public void ShouldRenderCreatedByMeStatusChangedWorkOrders()
    {
        using var ctx = new BunitContext();
        SetupJsInterop(ctx);

        var creator = new Employee("creator_user", "Creator", "User", "creator@example.com");
        var assignee = new Employee("assigned_user", "Assigned", "User", "assigned@example.com");
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-003",
            Title = "Status Changed",
            Creator = creator,
            Assignee = assignee,
            Status = WorkOrderStatus.InProgress,
            AssignedDate = DateTime.UtcNow.AddDays(-1)
        };

        var data = new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel(
            [],
            [],
            [workOrder]);

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, data));

        component.Markup.ShouldContain("WO-003");
        component.Markup.ShouldContain("Status Changed");
        component.Markup.ShouldContain("Assigned to");
    }

    private static WorkOrder BuildOrder(DateOnly? dueDate) => new()
    {
        Id = Guid.NewGuid(),
        Number = "WO-DUE",
        Title = "Due Test",
        Creator = new Employee("creator_user", "Creator", "User", "creator@example.com"),
        Status = WorkOrderStatus.Assigned,
        DueDate = dueDate
    };

    [TestCase(-3, "overdue", "Overdue")]
    [TestCase(0, "due-today", "Due today")]
    [TestCase(1, "upcoming", "Due tomorrow")]
    public void ShouldRenderDueUrgencyAndText(int offsetDays, string urgency, string text)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var due = DateOnly.FromDateTime(DateTime.Today).AddDays(offsetDays);
        var data = new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel([BuildOrder(due)], [], []);

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, data));

        var span = component.Find(".card-due-date");
        span.GetAttribute("data-due-urgency").ShouldBe(urgency);
        span.TextContent.Trim().ShouldBe(text);
    }

    [Test]
    public void ShouldRenderMonthDay_WhenDueMoreThanOneDayAway()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var due = DateOnly.FromDateTime(DateTime.Today).AddDays(5);
        var data = new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel([BuildOrder(due)], [], []);

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, data));

        var span = component.Find(".card-due-date");
        span.GetAttribute("data-due-urgency").ShouldBe("upcoming");
        span.TextContent.Trim().ShouldBe(due.ToString("M/d"));
    }

    [Test]
    public void ShouldRenderNoDueDate_WhenDueDateIsNull()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var data = new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel([BuildOrder(null)], [], []);

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, data));

        var span = component.Find(".card-due-date");
        span.GetAttribute("data-due-urgency").ShouldBe("none");
        span.TextContent.Trim().ShouldBe("No due date");
    }

    [Test]
    public async Task HandleEscapeKey_ShouldInvokeOnClose()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var closed = 0;
        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel([], [], []))
            .Add(c => c.OnClose, EventCallback.Factory.Create(this, () => closed++)));

        await component.InvokeAsync(() => component.Instance.HandleEscapeKey());

        closed.ShouldBe(1);
    }

    [Test]
    public async Task Dispose_ShouldCleanUpEscapeHandler_AfterOpening()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel([], [], [])));
        await component.WaitForAssertionAsync(() => ctx.JSInterop.Invocations.Any(i => i.Identifier == "setupEscapeHandler").ShouldBeTrue());

        await ctx.DisposeAsync();

        ctx.JSInterop.Invocations.Any(i => i.Identifier == "cleanupEscapeHandler").ShouldBeTrue();
    }

    [Test]
    public async Task ShouldInvokeOnClose_WhenEntryIsClicked()
    {
        await using var ctx = new BunitContext();
        SetupJsInterop(ctx);
        var closed = false;
        var workOrder = new WorkOrder { Number = "WO-1", Title = "Fix", Status = WorkOrderStatus.Assigned };

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel([], [workOrder], []))
            .Add(c => c.OnClose, () => closed = true));

        await component.Find(".notification-card").ClickAsync(new());

        closed.ShouldBeTrue();
    }
}
