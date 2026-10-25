using System.Text.RegularExpressions;
using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class GlobalSearchBoxTests
{
    [Test]
    public async Task ShouldNavigateToSearchWithQ_WhenEnterPressed()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<GlobalSearchBox>();
        var input = component.Find($"[data-testid='{GlobalSearchBox.Elements.GlobalSearchInput}']");

        await input.InputAsync(new ChangeEventArgs { Value = "organ loft" });
        await component.Find($"[data-testid='{GlobalSearchBox.Elements.GlobalSearchInput}']")
            .KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        ctx.Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/workorder/search?Q=organ%20loft");
    }

    [Test]
    public async Task ShouldIgnoreOtherKeys_AndExposeSearchLandmark()
    {
        await using var ctx = CreateContext();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var before = navigation.Uri;
        var component = ctx.Render<GlobalSearchBox>();

        await component.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "a" });

        navigation.Uri.ShouldBe(before);
        component.Find("[role='search']").ShouldNotBeNull();
        component.Find($"label[for='{GlobalSearchBox.InputId}']").TextContent.ShouldBe("Search work orders");
        component.Find("kbd").TextContent.ShouldBe("Ctrl K");
    }

    [Test]
    public async Task ShouldShowSlashHint_WhenRendered_AnnouncesPressSlashToSearch()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<GlobalSearchBox>();
        var input = component.Find($"[data-testid='{GlobalSearchBox.Elements.GlobalSearchInput}']");

        var shortcuts = input.GetAttribute("aria-keyshortcuts");
        shortcuts.ShouldNotBeNull();
        shortcuts.ShouldContain("/");

        var hintId = input.GetAttribute("aria-describedby");
        hintId.ShouldNotBeNullOrWhiteSpace();
        var announced = component.Find($"#{hintId}");
        announced.TextContent.ShouldBe("Press / to search");
        announced.ClassList.ShouldContain("visually-hidden");

        var visible = component.Find($"[data-testid='{GlobalSearchBox.Elements.GlobalSearchSlashHint}']");
        visible.TextContent.ShouldBe("Press / to search");
        visible.GetAttribute("aria-hidden").ShouldBe("true");
    }

    [TestCase(null, "workorder/search")]
    [TestCase("   ", "workorder/search")]
    [TestCase(" WO-1 ", "workorder/search?Q=WO-1")]
    public void WorkOrderSearchUrl_EncodesTrimmedText(string? text, string expected)
    {
        GlobalSearchBox.WorkOrderSearchUrl(text).ShouldBe(expected);
    }

    [Test]
    public async Task ShouldRegisterCtrlKShortcut_AfterFirstRender()
    {
        await using var ctx = CreateContext();
        var module = ctx.JSInterop.SetupModule("./_content/ClearMeasure.Bootcamp.UI.Shared/js/mainLayoutNav.js");
        module.SetupModule(invocation => invocation.Identifier == "initGlobalSearchShortcut");

        ctx.Render<GlobalSearchBox>();

        module.VerifyInvoke("initGlobalSearchShortcut");
    }

    [Test]
    public async Task ShouldKeepSearchTestIds_WhenToggleClicked_AndNotNavigate()
    {
        await using var ctx = CreateContext();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var before = navigation.Uri;
        var component = ctx.Render<GlobalSearchBox>();

        var toggle = component.Find($"[data-testid='{GlobalSearchBox.Elements.GlobalSearchToggle}']");
        toggle.GetAttribute("type").ShouldBe("button");
        toggle.GetAttribute("aria-label").ShouldBe("Search work orders");
        toggle.InnerHtml.ShouldContain("bi-search");
        component.Find($"[data-testid='{GlobalSearchBox.Elements.GlobalSearchInput}']").ShouldNotBeNull();
        component.Find($"[data-testid='{GlobalSearchBox.Elements.GlobalSearchSlashHint}']").TextContent.ShouldBe("Press / to search");

        await toggle.ClickAsync(new MouseEventArgs());

        navigation.Uri.ShouldBe(before);
        component.Find($"#{GlobalSearchBox.InputId}").ShouldNotBeNull();
    }

    [Test]
    public void ShouldCollapseSearchWithoutNewColors_InPhoneWidthStyles()
    {
        var searchCss = File.ReadAllText(RepoFile("UI.Shared", "Components", "GlobalSearchBox.razor.css"));
        var layoutCss = File.ReadAllText(RepoFile("UI.Shared", "MainLayout.razor.css"));

        searchCss.ShouldContain(".global-search-toggle");
        searchCss.ShouldContain("max-width: 480px");
        searchCss.ShouldContain(".global-search:focus-within .global-search-field");
        searchCss.ShouldContain("var(--color-surface)");
        searchCss.ShouldContain("var(--shadow-popover)");
        Regex.IsMatch(searchCss, "#[0-9a-fA-F]{3,8}").ShouldBeFalse();

        layoutCss.ShouldContain("flex-wrap: nowrap");
        layoutCss.ShouldContain("order: 0");
    }

    private static string RepoFile(params string[] parts)
    {
        var path = Path.GetFullPath(Path.Combine(
            new[] { NUnit.Framework.TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".." }.Concat(parts).ToArray()));
        File.Exists(path).ShouldBeTrue(path);
        return path;
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        return ctx;
    }
}
