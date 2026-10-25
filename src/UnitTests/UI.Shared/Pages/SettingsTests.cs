using System.Text.RegularExpressions;
using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class SettingsTests
{
    [Test]
    public async Task Settings_AuthenticatedUser_ShouldRenderAppearanceSectionAndDarkModeSwitch()
    {
        await using var ctx = CreateContext();
        var module = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        module.Setup<string>("getTheme").SetResult("light");
        module.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());

        component.Find("h2").TextContent.ShouldContain("Appearance");
        var sw = component.Find($"[data-testid='{nameof(Settings.Elements.DarkModeSwitch)}']");
        sw.GetAttribute("role").ShouldBe("switch");
        component.Markup.ShouldContain("Dark mode");
        component.Markup.ShouldContain("whole app");
    }

    [Test]
    public async Task Settings_AuthenticatedUser_ShouldRenderDisplayPreferencesSubtitle()
    {
        await using var ctx = CreateContext();
        var module = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        module.Setup<string>("getTheme").SetResult("light");
        module.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());

        var subtitle = component.Find($"[data-testid='{nameof(Settings.Elements.SettingsSubtitle)}']");
        subtitle.TextContent.ShouldBe("Manage display preferences for this browser.");
    }

    [Test]
    public async Task Settings_ToggleDarkMode_ShouldInvokeSetThemeInteropAndUpdateServiceState()
    {
        await using var ctx = CreateContext();
        var module = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        module.Setup<string>("getTheme").SetResult("light");
        module.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();
        module.SetupVoid("setTheme", _ => true).SetVoidResult();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());
        var theme = ctx.Services.GetRequiredService<ThemePreferenceService>();

        await component.InvokeAsync(() => Task.CompletedTask);
        theme.IsDarkMode.ShouldBeFalse();

        var sw = component.Find($"[data-testid='{nameof(Settings.Elements.DarkModeSwitch)}']");
        await sw.ChangeAsync(new() { Value = true });

        await component.WaitForAssertionAsync(() => theme.IsDarkMode.ShouldBeTrue());
        module.VerifyInvoke("setTheme");
    }

    [Test]
    public async Task Settings_DarkModeStatus_WhenThemeIsLight_ShouldShowOff()
    {
        await using var ctx = CreateContext();
        var module = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        module.Setup<string>("getTheme").SetResult("light");
        module.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());

        var status = component.Find($"[data-testid='{nameof(Settings.Elements.DarkModeStatus)}']");
        status.TextContent.ShouldBe("Dark mode is off.");
    }

    [Test]
    public async Task Settings_DarkModeStatus_WhenToggledOn_ShouldShowOn()
    {
        await using var ctx = CreateContext();
        var module = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        module.Setup<string>("getTheme").SetResult("light");
        module.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();
        module.SetupVoid("setTheme", _ => true).SetVoidResult();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());

        var sw = component.Find($"[data-testid='{nameof(Settings.Elements.DarkModeSwitch)}']");
        await sw.ChangeAsync(new() { Value = true });

        await component.WaitForAssertionAsync(() =>
        {
            var status = component.Find($"[data-testid='{nameof(Settings.Elements.DarkModeStatus)}']");
            status.TextContent.ShouldBe("Dark mode is on.");
        });
    }

    [Test]
    public async Task Settings_AuthenticatedUser_ShouldRenderPageHeaderAndTokenCard()
    {
        await using var ctx = CreateContext();
        var module = ctx.JSInterop.SetupModule(ThemePreferenceService.ThemeJsModulePath);
        module.Setup<string>("getTheme").SetResult("light");
        module.SetupVoid("syncDomFromTheme", _ => true).SetVoidResult();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Settings>());

        component.FindComponents<PageHeader>().Count.ShouldBe(1);
        component.Find("h1").TextContent.ShouldBe("Settings");
        component.Find(".token-card").ShouldNotBeNull();
        component.Find($"[data-testid='{nameof(Settings.Elements.DarkModeSwitch)}']").GetAttribute("role").ShouldBe("switch");
    }

    [Test]
    public void Settings_Stylesheet_ShouldUseDesignTokensWithoutDarkOverrides()
    {
        var path = Path.GetFullPath(Path.Combine(
            NUnit.Framework.TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "UI.Shared", "Pages", "Settings.razor.css"));
        File.Exists(path).ShouldBeTrue($"Expected scoped stylesheet at {path}");
        var css = File.ReadAllText(path);

        css.ShouldContain(".token-card");
        css.ShouldContain("var(--color-surface)");
        css.ShouldContain("var(--color-text)");
        css.ShouldContain("var(--font-sans)");
        css.ShouldNotContain("Georgia");
        css.ShouldNotContain("html[data-theme=\"dark\"]");
        Regex.IsMatch(css, "#[0-9a-fA-F]{3,8}").ShouldBeFalse();
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;
        ctx.Services.AddSingleton(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton<ThemePreferenceService>();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var bunitAuth = ctx.AddAuthorization();
        bunitAuth.SetAuthorized("hsimpson");
        var customAuth = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        customAuth.Login("hsimpson").GetAwaiter().GetResult();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(customAuth);
        ctx.Services.AddSingleton(customAuth);

        return ctx;
    }
}
