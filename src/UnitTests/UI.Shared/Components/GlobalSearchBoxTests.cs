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

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        return ctx;
    }
}
