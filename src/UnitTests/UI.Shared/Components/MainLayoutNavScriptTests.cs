using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.JSInterop;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class MainLayoutNavScriptTests
{
    [Test]
    public async Task ShouldImportTheModuleOnce_WhenInitNavToggleIsCalledTwice()
    {
        await using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(MainLayoutNavScript.ModulePath);
        module.SetupModule(invocation => invocation.Identifier == "initNavToggle");
        var script = new MainLayoutNavScript(ctx.JSInterop.JSRuntime);
        using var dotNet = DotNetObjectReference.Create(new NavRail());

        await script.InitNavToggleAsync(dotNet, NavRail.BreakpointMediaQuery, () => false);
        await script.InitNavToggleAsync(dotNet, NavRail.BreakpointMediaQuery, () => false);

        ctx.JSInterop.VerifyInvoke("import", calledTimes: 1);
    }

    [Test]
    public async Task ShouldSkipInitNavToggle_WhenDisposedDuringImport()
    {
        await using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(MainLayoutNavScript.ModulePath);
        module.SetupModule(invocation => invocation.Identifier == "initNavToggle");
        var script = new MainLayoutNavScript(ctx.JSInterop.JSRuntime);
        using var dotNet = DotNetObjectReference.Create(new NavRail());

        await script.InitNavToggleAsync(dotNet, NavRail.BreakpointMediaQuery, () => true);

        module.Invocations.ShouldBeEmpty();
    }
}
