using Bunit;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.JsModules;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Shell;

[TestFixture]
public class NavRailJsDisconnectTests
{
    [Test]
    public async Task ShouldSurvive_WhenModuleImportDisconnectsDuringInit()
    {
        await using var ctx = ShellLayoutContext.Create();
        ctx.Services.AddSingleton<IJSRuntime>(new DisconnectingJsRuntime(failImport: true));

        var layout = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());

        layout.FindComponent<NavRail>().ShouldNotBeNull();
        layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldSurvive_WhenModuleDisposeDisconnects()
    {
        await using var ctx = ShellLayoutContext.Create();
        ctx.Services.AddSingleton<IJSRuntime>(new DisconnectingJsRuntime(failDispose: true));
        ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());

        await Should.NotThrowAsync(async () => await ctx.DisposeComponentsAsync());
    }
}
