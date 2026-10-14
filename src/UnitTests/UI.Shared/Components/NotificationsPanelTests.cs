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
    private static void SetupJSInterop(BunitContext ctx)
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
        SetupJSInterop(ctx);

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel([], [], [])));

        component.Markup.ShouldContain("class=\"notifications-panel\"");
    }

    [Test]
    public void ShouldRenderAllThreeSections_WhenOpen()
    {
        using var ctx = new BunitContext();
        SetupJSInterop(ctx);

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
        SetupJSInterop(ctx);

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, new global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel([], [], [])));

        component.Markup.ShouldContain("No overdue or due-today work orders");
    }

    [Test]
    public void ShouldRenderWorkOrderCards_WhenOverdueAndDueTodayHasItems()
    {
        using var ctx = new BunitContext();
        SetupJSInterop(ctx);

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
            new[] { workOrder },
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
        SetupJSInterop(ctx);

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
            new[] { workOrder },
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
        SetupJSInterop(ctx);

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
            new[] { workOrder },
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
        SetupJSInterop(ctx);

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
            new[] { workOrder });

        var component = ctx.Render<global::ClearMeasure.Bootcamp.UI.Shared.Components.NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, data));

        component.Markup.ShouldContain("WO-003");
        component.Markup.ShouldContain("Status Changed");
        component.Markup.ShouldContain("Assigned to");
    }
}
