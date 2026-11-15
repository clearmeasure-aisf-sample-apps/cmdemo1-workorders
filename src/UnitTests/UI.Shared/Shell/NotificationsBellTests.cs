using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Shell;

[TestFixture]
public class NotificationsBellTests
{
    [Test]
    public async Task NotificationsButton_ShouldNotBeDisabled_WhenUserIsAuthenticated()
    {
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson");
        var layout = RenderLayout(ctx);

        var button = FindBell(layout);
        button.GetAttribute("aria-disabled").ShouldBeNull();
        button.GetAttribute("title").ShouldBe("Notifications");
    }

    [Test]
    public async Task NotificationsButton_AccessibleName_ShouldBeNotifications_WhenBadgeCountIsZero()
    {
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson");
        var layout = RenderLayout(ctx);

        var button = FindBell(layout);
        button.GetAttribute("aria-label").ShouldBe("Notifications");
        button.GetAttribute("title").ShouldBe("Notifications");
    }

    [Test]
    public async Task NotificationsButton_AccessibleName_ShouldIncludeUnreadCount_WhenBadgeCountIsPositive()
    {
        WorkOrder[] orders =
        [
            new() { Number = "WO-1", Title = "A", Status = WorkOrderStatus.Assigned },
            new() { Number = "WO-2", Title = "B", Status = WorkOrderStatus.InProgress },
            new() { Number = "WO-3", Title = "C", Status = WorkOrderStatus.Draft }
        ];
        var bus = new NotificationsStubBus(new NotificationsSummary(orders, [], []));
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson", bus: bus);
        var layout = RenderLayout(ctx);

        await layout.WaitForAssertionAsync(() =>
        {
            var button = FindBell(layout);
            button.GetAttribute("aria-label").ShouldBe("Notifications (3 unread)");
            button.GetAttribute("title").ShouldBe("Notifications (3 unread)");
        });
    }

    [Test]
    public async Task NotificationsBadge_ShouldNotRender_WhenCountIsZero()
    {
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson");
        var layout = RenderLayout(ctx);

        layout.FindAll($"[data-testid='{nameof(MainLayout.Elements.NotificationsBadge)}']").Count.ShouldBe(0);
    }

    [Test]
    public async Task NotificationsBell_ShouldOpenPanelAndShowBadge_WhenUserHasOverdueWork()
    {
        var order = new WorkOrder { Number = "WO-9", Title = "Late", Status = WorkOrderStatus.Assigned };
        var bus = new NotificationsStubBus(new NotificationsSummary([order], [], []));
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson", bus: bus);
        var layout = RenderLayout(ctx);

        await FindBell(layout).ClickAsync(new());

        await layout.WaitForAssertionAsync(() => layout.Find(".notifications-panel").ShouldNotBeNull());
        bus.LastUserName.ShouldBe("hsimpson");
        layout.Find($"[data-testid='{nameof(MainLayout.Elements.NotificationsBadge)}']").TextContent.Trim().ShouldBe("1");
    }

    [Test]
    public async Task NotificationsBadge_ShouldRenderOnInitialLoad_WithOverdueAndDueTodayCount()
    {
        WorkOrder[] orders =
        [
            new() { Number = "WO-1", Title = "A", Status = WorkOrderStatus.Assigned },
            new() { Number = "WO-2", Title = "B", Status = WorkOrderStatus.InProgress },
            new() { Number = "WO-3", Title = "C", Status = WorkOrderStatus.Draft }
        ];
        var bus = new NotificationsStubBus(new NotificationsSummary(orders, [], []));
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson", bus: bus);
        var layout = RenderLayout(ctx);

        await layout.WaitForAssertionAsync(() =>
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NotificationsBadge)}']").TextContent.Trim().ShouldBe("3"));
        layout.FindAll(".notifications-panel").Count.ShouldBe(0);
    }

    [Test]
    public async Task NotificationsBell_ShouldNotOpenPanel_WhenUserNameIsMissing()
    {
        var bus = new NotificationsStubBus(new NotificationsSummary([], [], []));
        await using var ctx = ShellLayoutContext.Create(bus: bus);
        var layout = RenderLayout(ctx);

        await FindBell(layout).ClickAsync(new());

        layout.FindAll(".notifications-panel").Count.ShouldBe(0);
        bus.LastUserName.ShouldBeNull();
    }

    [Test]
    public async Task NotificationsBell_ShouldNotOpenPanel_WhenQueryFails()
    {
        var bus = new NotificationsStubBus(null);
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson", bus: bus);
        var layout = RenderLayout(ctx);

        await FindBell(layout).ClickAsync(new());

        layout.FindAll(".notifications-panel").Count.ShouldBe(0);
    }

    [Test]
    public async Task NotificationsPanel_ShouldClose_WhenEscapeIsInvoked()
    {
        var bus = new NotificationsStubBus(new NotificationsSummary([], [], []));
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson", bus: bus);
        var layout = RenderLayout(ctx);
        await FindBell(layout).ClickAsync(new());
        var panel = await layout.WaitForElementAsync(".notifications-panel");
        panel.ShouldNotBeNull();

        var panelComponent = layout.FindComponent<NotificationsPanel>();
        await layout.InvokeAsync(() => panelComponent.Instance.HandleEscapeKey());

        await layout.WaitForAssertionAsync(() => layout.FindAll(".notifications-panel").Count.ShouldBe(0));
        FindBell(layout).GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Test]
    public async Task NotificationsPanel_ShouldClose_WhenLocationChanges()
    {
        var order = new WorkOrder { Number = "WO-9", Title = "Late", Status = WorkOrderStatus.Assigned };
        var bus = new NotificationsStubBus(new NotificationsSummary([order], [], []));
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson", bus: bus);
        var layout = RenderLayout(ctx);
        await FindBell(layout).ClickAsync(new());
        await layout.WaitForElementAsync(".notifications-panel");

        await layout.InvokeAsync(() => ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/workorder/manage/WO-9?mode=Edit"));

        await layout.WaitForAssertionAsync(() => layout.FindAll(".notifications-panel").Count.ShouldBe(0));
        FindBell(layout).GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Test]
    public async Task NotificationsPanel_ShouldClose_WhenEntryIsClicked()
    {
        var order = new WorkOrder { Number = "WO-9", Title = "Late", Status = WorkOrderStatus.Assigned };
        var bus = new NotificationsStubBus(new NotificationsSummary([order], [], []));
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson", bus: bus);
        var layout = RenderLayout(ctx);
        await FindBell(layout).ClickAsync(new());
        await layout.WaitForElementAsync(".notifications-panel");

        await layout.Find(".notification-card").ClickAsync(new());

        await layout.WaitForAssertionAsync(() => layout.FindAll(".notifications-panel").Count.ShouldBe(0));
        FindBell(layout).GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Test]
    public async Task NotificationsBadge_ShouldShow_WhenUserSignsInAfterInitialAnonymousRender()
    {
        WorkOrder[] orders = [new() { Number = "WO-1" }, new() { Number = "WO-2" }];
        var bus = new NotificationsStubBus(new NotificationsSummary(orders, [], []));
        var session = new StubUserSession();
        await using var ctx = ShellLayoutContext.Create(bus: bus, userSession: session);
        var layout = RenderLayout(ctx);
        layout.FindAll($"[data-testid='{nameof(MainLayout.Elements.NotificationsBadge)}']").Count.ShouldBe(0);
        bus.LastUserName.ShouldBeNull();

        await layout.InvokeAsync(() =>
        {
            session.UserName = "mflanders";
            session.RaiseAuthenticationStateChanged();
        });

        await layout.WaitForAssertionAsync(() =>
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NotificationsBadge)}']").TextContent.Trim().ShouldBe("2"));
        bus.LastUserName.ShouldBe("mflanders");
    }

    [Test]
    public async Task NotificationsBadge_ShouldRefresh_WhenLocationChanges()
    {
        var bus = new NotificationsStubBus(new NotificationsSummary([], [], []));
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson", bus: bus);
        var layout = RenderLayout(ctx);
        layout.FindAll($"[data-testid='{nameof(MainLayout.Elements.NotificationsBadge)}']").Count.ShouldBe(0);

        bus.Result = new NotificationsSummary([new WorkOrder { Number = "WO-3" }], [], []);
        await layout.InvokeAsync(() => ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/workorder/search"));

        await layout.WaitForAssertionAsync(() =>
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NotificationsBadge)}']").TextContent.Trim().ShouldBe("1"));
    }

    private static IRenderedComponent<CascadingAuthenticationState> RenderLayout(BunitContext ctx) =>
        ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());

    private static AngleSharp.Dom.IElement FindBell(IRenderedComponent<CascadingAuthenticationState> layout) =>
        layout.Find($"[data-testid='{nameof(MainLayout.Elements.NotificationsButton)}']");
}
