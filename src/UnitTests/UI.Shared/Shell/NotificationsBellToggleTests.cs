using Bunit;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Shell;

[TestFixture]
public class NotificationsBellToggleTests
{
    [Test]
    public async Task ShouldRenderBellButton_WithNotificationsAccessibleName()
    {
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson");
        var layout = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());

        var toggle = layout.FindComponent<NotificationsBellToggle>();
        var button = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NotificationsButton)}']");

        toggle.Instance.ShouldNotBeNull();
        button.GetAttribute("aria-label").ShouldBe("Notifications");
        button.GetAttribute("aria-expanded").ShouldBe("false");
    }
}
