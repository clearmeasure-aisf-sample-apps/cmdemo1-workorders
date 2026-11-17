using Bunit;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Shell;

[TestFixture]
public class NavRailTests
{
    [Test]
    public async Task ShouldRenderNavigationRailAsideWithAccessibleName()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);

        layout.Find("#app-navigation-rail").GetAttribute("aria-label").ShouldBe("Application sidebar");
    }

    [Test]
    public async Task ShouldRenderNavRailToggleWithExpandedStateByDefault()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        toggle.GetAttribute("aria-expanded").ShouldBe("true");
        toggle.GetAttribute("aria-controls").ShouldBe("app-navigation-rail");
        toggle.GetAttribute("title")!.ShouldContain("Hide");
        toggle.GetAttribute("aria-label")!.ShouldContain("Hide");
        layout.Find("#app-navigation-rail").ClassList.ShouldContain("modern-sidebar");
        layout.Find(".modern-app").ClassList.ShouldNotContain("rail-collapsed");
    }

    [Test]
    public async Task ShouldToggleNavRailCollapseAndUpdateAriaOnWideLayout()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);
        var rail = layout.FindComponent<NavRail>();
        await WaitForToggle(layout);
        await SetViewport(layout, rail, isNarrow: false);

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        await toggle.ClickAsync(new());

        toggle.GetAttribute("aria-expanded").ShouldBe("false");
        toggle.GetAttribute("title")!.ShouldContain("Show");
        layout.Find(".modern-app").ClassList.ShouldContain("rail-collapsed");
        layout.Find("#app-navigation-rail").ClassList.ShouldContain("rail-hidden");

        await toggle.ClickAsync(new());

        toggle.GetAttribute("aria-expanded").ShouldBe("true");
        toggle.GetAttribute("title")!.ShouldContain("Hide");
        layout.Find(".modern-app").ClassList.ShouldNotContain("rail-collapsed");
        layout.Find("#app-navigation-rail").ClassList.ShouldNotContain("rail-hidden");
    }

    [Test]
    public async Task ShouldRenderCorrectIconForNavVisibility()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);
        var rail = layout.FindComponent<NavRail>();
        await WaitForToggle(layout);
        await SetViewport(layout, rail, isNarrow: false);

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        toggle.InnerHtml.ShouldContain("bi-chevron-double-left");

        await toggle.ClickAsync(new());

        toggle.InnerHtml.ShouldContain("bi-list");
    }

    [Test]
    public async Task ShouldUseOverlayOpenClassOnNarrowViewportWhenNavVisible()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);
        var rail = layout.FindComponent<NavRail>();
        await WaitForToggle(layout);
        await SetViewport(layout, rail, isNarrow: true);

        var sidebar = layout.Find("#app-navigation-rail");
        sidebar.ClassList.ShouldNotContain("open");

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        await toggle.ClickAsync(new());

        sidebar.ClassList.ShouldContain("open");
        toggle.GetAttribute("aria-expanded").ShouldBe("true");
    }

    [Test]
    public void ShouldUseDocumentedNavRailBreakpointMediaQuery()
    {
        MainLayout.NavRailBreakpointMediaQuery.ShouldBe("(max-width: 768px)");
        NavRail.BreakpointMediaQuery.ShouldBe("(max-width: 768px)");
    }

    [Test]
    public async Task ShouldInvokeFocusOnNavRailToggleWhenClosingOverlayOnNarrowViewport()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);
        var rail = layout.FindComponent<NavRail>();
        await WaitForToggle(layout);
        await SetViewport(layout, rail, isNarrow: true);

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        await toggle.ClickAsync(new());
        await toggle.ClickAsync(new());

        ctx.JSInterop.VerifyFocusAsyncInvoke();
    }

    [Test]
    public async Task ShouldRenderBackdrop_WhenNarrowAndNavOpen()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);
        var rail = layout.FindComponent<NavRail>();
        await WaitForToggle(layout);
        await SetViewport(layout, rail, isNarrow: true);

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        await toggle.ClickAsync(new());

        layout.FindAll(".nav-backdrop").Count.ShouldBe(1);
        layout.Find("#app-navigation-rail").ClassList.ShouldContain("open");
    }

    [Test]
    public async Task ShouldNotRenderBackdrop_WhenNarrowAndNavClosed()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);
        var rail = layout.FindComponent<NavRail>();
        await WaitForToggle(layout);
        await SetViewport(layout, rail, isNarrow: true);

        layout.FindAll(".nav-backdrop").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldNotRenderBackdrop_OnWideViewport()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);
        var rail = layout.FindComponent<NavRail>();
        await WaitForToggle(layout);
        await SetViewport(layout, rail, isNarrow: false);

        layout.FindAll(".nav-backdrop").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldDismissNavWhenBackdropClicked()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);
        var rail = layout.FindComponent<NavRail>();
        await WaitForToggle(layout);
        await SetViewport(layout, rail, isNarrow: true);

        var toggle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']");
        await toggle.ClickAsync(new());

        layout.FindAll(".nav-backdrop").Count.ShouldBe(1);

        var backdrop = layout.Find(".nav-backdrop");
        await backdrop.ClickAsync(new());

        layout.Find("#app-navigation-rail").ClassList.ShouldNotContain("open");
        layout.FindAll(".nav-backdrop").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldNotInitializeNavToggleWhenDisposedWhileModuleLoads()
    {
        await using var ctx = ShellLayoutContext.Create();
        var js = new PendingImportJsRuntime();
        ctx.Services.AddSingleton<IJSRuntime>(js);
        ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());

        await ctx.DisposeComponentsAsync();
        var module = new RecordingJsModule();
        js.Import.SetResult(module);

        module.Identifiers.ShouldNotContain("initNavToggle");
    }

    private static IRenderedComponent<CascadingAuthenticationState> RenderLayout(BunitContext ctx) =>
        ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());

    private static Task WaitForToggle(IRenderedComponent<CascadingAuthenticationState> layout) =>
        layout.WaitForAssertionAsync(() =>
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NavRailToggle)}']").ShouldNotBeNull());

    private static Task SetViewport(
        IRenderedComponent<CascadingAuthenticationState> layout,
        IRenderedComponent<NavRail> rail,
        bool isNarrow) =>
        layout.InvokeAsync(() => rail.Instance.OnViewportChanged(isNarrow));

    private sealed class PendingImportJsRuntime : IJSRuntime
    {
        public TaskCompletionSource<IJSObjectReference> Import { get; } = new();

#pragma warning disable BL0016 // -- reason: test fake lets JS exceptions propagate to callers that catch them
        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "import" && args?[0] is string path && path.EndsWith("mainLayoutNav.js"))
                return (TValue)await Import.Task;
            if (identifier == "import")
                return (TValue)(object)new RecordingJsModule();
            return default!;
        }
#pragma warning restore BL0016

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) =>
#pragma warning disable BL0016 // -- reason: test fake lets JS exceptions propagate to callers that catch them
            InvokeAsync<TValue>(identifier, args);
#pragma warning restore BL0016
    }

    private sealed class RecordingJsModule : IJSObjectReference
    {
        public List<string> Identifiers { get; } = [];

#pragma warning disable BL0016 // -- reason: test fake lets JS exceptions propagate to callers that catch them
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Identifiers.Add(identifier);
            return ValueTask.FromResult(default(TValue)!);
        }
#pragma warning restore BL0016

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) =>
#pragma warning disable BL0016 // -- reason: test fake lets JS exceptions propagate to callers that catch them
            InvokeAsync<TValue>(identifier, args);
#pragma warning restore BL0016

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
