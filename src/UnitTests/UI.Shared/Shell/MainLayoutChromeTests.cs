using Bunit;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Shell;

[TestFixture]
public class MainLayoutChromeTests
{
    [Test]
    public async Task ShouldRenderSidebarBrandAsDotNetConfWorkOrders()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);

        var brandText = layout.Find(".brand-text");
        brandText.QuerySelector("h4")!.TextContent.Trim().ShouldBe(".NET Conf Work Orders");
        brandText.TextContent.ShouldNotContain("SoundPhysicians");
        layout.Markup.ShouldNotContain("Church Bulletin");
        brandText.TextContent.ShouldNotContain("First Church");
        brandText.TextContent.ShouldNotContain("Springfield");
    }

    [Test]
    public async Task ShouldRenderBrandSubtitle()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);

        var subtitle = layout.Find($"[data-testid='{nameof(MainLayout.Elements.BrandSubtitle)}']");
        subtitle.TextContent.Trim().ShouldBe("Kalahari Church · Facilities");
        subtitle.Closest(".brand-text").ShouldBeNull();
        layout.Find(".church-brand").QuerySelector(".brand-mark").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldRenderBreadcrumbsInsteadOfAdministrationTitle()
    {
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson");
        var layout = RenderLayout(ctx);

        layout.Markup.ShouldNotContain("Work Order Administration");
        layout.FindAll(".header-title h3").Count.ShouldBe(0);
        var breadcrumbs = layout.Find("nav[aria-label='Breadcrumb']");
        breadcrumbs.TextContent.ShouldContain("Dashboard");
        layout.Find("[role='search']").ShouldNotBeNull();
        layout.Find($"[data-testid='{nameof(MainLayout.Elements.HelpLink)}']").GetAttribute("aria-label").ShouldBe("Help");
        var notifButton = layout.Find($"[data-testid='{nameof(MainLayout.Elements.NotificationsButton)}']");
        notifButton.ShouldNotBeNull();
        notifButton.GetAttribute("aria-disabled").ShouldBeNull();
    }

    [Test]
    public async Task ShouldRenderSkipToMainContentLinkAsFirstLink()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);

        var firstLink = layout.Find("a");
        firstLink.TextContent.Trim().ShouldBe("Skip to main content");
        firstLink.GetAttribute("href").ShouldBe("#main-content");

        var main = layout.Find("main.modern-main");
        main.GetAttribute("id").ShouldBe("main-content");
        main.GetAttribute("tabindex").ShouldBe("-1");
    }

    [Test]
    public async Task ShouldRenderLoginLink_WithBlinkClass_WhenUserIsNotAuthenticated()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);

        var loginAnchor = layout.Find($"a[data-testid='{nameof(LoginLink.Elements.LoginLink)}']");
        loginAnchor.GetAttribute("data-testid").ShouldBe(nameof(LoginLink.Elements.LoginLink));
        loginAnchor.ClassList.ShouldContain("login-link-blink");
        loginAnchor.GetAttribute("id").ShouldBe("login-link-blink");
        loginAnchor.TextContent.Trim().ShouldBe("Login");
        loginAnchor.GetAttribute("href").ShouldBe("/login");
    }

    [Test]
    public async Task ShouldExposeBlinkIdOnSameAnchorAsBlinkClassAndTestId_WhenUserIsNotAuthenticated()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);

        var anchorsById = layout.FindAll("#login-link-blink");
        anchorsById.Count.ShouldBe(1);

        var loginAnchor = anchorsById[0];
        loginAnchor.TagName.ShouldBe("A");
        loginAnchor.ClassList.ShouldContain("login-link-blink");
        loginAnchor.GetAttribute("data-testid").ShouldBe(nameof(LoginLink.Elements.LoginLink));
        loginAnchor.GetAttribute("href").ShouldBe("/login");
    }

    [Test]
    public async Task ShouldNotExposeBlinkId_WhenUserIsAuthenticated()
    {
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson");
        var layout = RenderLayout(ctx);

        layout.FindAll("#login-link-blink").Count.ShouldBe(0);
        layout.Find($"[data-testid='{nameof(Logout.Elements.LogoutLink)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldPreserveLoginLinkHref_WhenBlinkClassApplied()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);

        var loginAnchor = layout.Find($"a[data-testid='{nameof(LoginLink.Elements.LoginLink)}']");
        loginAnchor.ClassList.ShouldContain("login-link-blink");
        loginAnchor.GetAttribute("href").ShouldBe("/login");
    }

    [Test]
    public async Task ShouldNotRenderLoginLink_WhenUserIsAuthenticated()
    {
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson");
        var layout = RenderLayout(ctx);

        layout.FindAll($"a[data-testid='{nameof(LoginLink.Elements.LoginLink)}']").Count.ShouldBe(0);
        layout.Find($"[data-testid='{nameof(Logout.Elements.LogoutLink)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldNotExposeBlinkClass_WhenUserIsAuthenticated()
    {
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson");
        var layout = RenderLayout(ctx);

        layout.FindAll(".login-link-blink").Count.ShouldBe(0);
        layout.Find($"[data-testid='{nameof(Logout.Elements.LogoutLink)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task MainLayout_AfterFirstRender_ShouldCallThemeInitialize_WhenImplemented()
    {
        await using var ctx = ShellLayoutContext.Create();
        var themeModule = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        themeModule.Setup<string>("getTheme").SetResult("light");
        themeModule.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();

        ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());

        themeModule.VerifyInvoke("getTheme");
    }

    [Test]
    public async Task DarkModeToggle_ShouldRender_WithSunIcon_WhenLightMode()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);

        var button = layout.Find($"[data-testid='{nameof(MainLayout.Elements.DarkModeToggle)}']");
        button.ShouldNotBeNull();
        button.InnerHtml.ShouldContain("bi-sun");
    }

    [Test]
    public async Task DarkModeToggle_ShouldRender_WithMoonIcon_WhenDarkMode()
    {
        await using var ctx = ShellLayoutContext.Create();
        var themeModule = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        themeModule.Setup<string>("getTheme").SetResult("dark");
        themeModule.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();
        themeModule.SetupVoid("setTheme", _ => true).SetVoidResult();

        var layout = RenderLayout(ctx);
        await layout.WaitForAssertionAsync(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.DarkModeToggle)}']").InnerHtml.ShouldContain("bi-moon");
        });
    }

    [Test]
    public async Task DarkModeToggle_Click_ShouldFlipIsDarkMode()
    {
        await using var ctx = ShellLayoutContext.Create();
        var themeModule = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        themeModule.Setup<string>("getTheme").SetResult("light");
        themeModule.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();
        themeModule.SetupVoid("setTheme", _ => true).SetVoidResult();

        var layout = RenderLayout(ctx);
        await layout.WaitForAssertionAsync(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.DarkModeToggle)}']").ShouldNotBeNull();
        });

        var button = layout.Find($"[data-testid='{nameof(MainLayout.Elements.DarkModeToggle)}']");
        button.InnerHtml.ShouldContain("bi-sun");

        await button.ClickAsync(new());

        await layout.WaitForAssertionAsync(() =>
        {
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.DarkModeToggle)}']").InnerHtml.ShouldContain("bi-moon");
        });
        ctx.Services.GetRequiredService<ThemePreferenceService>().IsDarkMode.ShouldBeTrue();
    }

    [Test]
    public async Task DarkModeToggle_ShouldBeVisible_WhenUserIsNotAuthenticated()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);

        layout.Find($"[data-testid='{nameof(MainLayout.Elements.DarkModeToggle)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task DarkModeToggle_ShouldBeVisible_WhenUserIsAuthenticated()
    {
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson");
        var layout = RenderLayout(ctx);

        layout.Find($"[data-testid='{nameof(MainLayout.Elements.DarkModeToggle)}']").ShouldNotBeNull();
    }

    private static IRenderedComponent<CascadingAuthenticationState> RenderLayout(BunitContext ctx) =>
        ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
}
