using Bunit;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Shell;

[TestFixture]
public class NavRailToggleTests
{
    [Test]
    public async Task ShouldRenderExpandedToggle_WhenNavRailIsVisible()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());

        var toggle = layout.FindComponent<NavRailToggle>();
        var button = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");

        toggle.Instance.ShouldNotBeNull();
        button.GetAttribute("aria-expanded").ShouldBe("true");
        button.GetAttribute("aria-controls").ShouldBe("app-navigation-rail");
    }
}
