using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.JsModules;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class GlobalSearchBoxJsDisconnectTests
{
    [Test]
    public async Task ShouldSurvive_WhenModuleImportDisconnectsDuringInit()
    {
        await using var ctx = CreateContext(new DisconnectingJsRuntime(failImport: true));

        var component = ctx.Render<GlobalSearchBox>();

        component.Find($"[data-testid='{GlobalSearchBox.Elements.GlobalSearchInput}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldSurvive_WhenModuleDisposeDisconnects()
    {
        await using var ctx = CreateContext(new DisconnectingJsRuntime(failDispose: true));
        ctx.Render<GlobalSearchBox>();

        await Should.NotThrowAsync(async () => await ctx.DisposeAsync());
    }

    private static BunitContext CreateContext(IJSRuntime js)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(js);
        return ctx;
    }
}
