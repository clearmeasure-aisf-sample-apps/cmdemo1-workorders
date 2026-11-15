using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Shell;

[TestFixture]
public class NotificationsBellPanelTests
{
    [Test]
    public async Task ShouldHostNotificationsPanel_WhenBellOpens()
    {
        var bus = new NotificationsStubBus(new NotificationsSummary(
            [new WorkOrder { Number = "WO-9", Title = "Late", Status = WorkOrderStatus.Assigned }],
            [],
            []));
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson", bus: bus);
        var layout = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());

        layout.FindComponent<NotificationsBellPanel>().ShouldNotBeNull();
        await layout.Find($"[data-testid='{nameof(MainLayout.Elements.NotificationsButton)}']").ClickAsync(new());

        await layout.WaitForAssertionAsync(() => layout.Find(".notifications-panel").ShouldNotBeNull());
        layout.FindComponent<NotificationsPanel>().ShouldNotBeNull();
    }
}
