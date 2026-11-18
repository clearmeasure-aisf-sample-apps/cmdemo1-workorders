using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class ClipboardScriptTests
{
    [Test]
    public async Task ShouldCopyText_WhenClipboardSucceeds()
    {
        await using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ClipboardScript.ModulePath);
        module.SetupVoid("copyToClipboard", _ => true).SetVoidResult();
        var script = new ClipboardScript(ctx.JSInterop.JSRuntime);

        var copied = await script.CopyToClipboardAsync("WO-1");

        copied.ShouldBeTrue();
        module.VerifyInvoke("copyToClipboard").Arguments.ShouldBe(["WO-1"]);
    }

    [Test]
    public async Task ShouldDisposeWithoutImport_WhenModuleNeverLoaded()
    {
        var script = new ClipboardScript(new DisconnectingJsRuntime(failImport: true));

        await Should.NotThrowAsync(async () => await script.DisposeAsync());
    }

    [Test]
    public async Task ShouldDisposeLoadedModule_WhenCopySucceeded()
    {
        await using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ClipboardScript.ModulePath);
        module.SetupVoid("copyToClipboard", _ => true).SetVoidResult();
        var script = new ClipboardScript(ctx.JSInterop.JSRuntime);
        await script.CopyToClipboardAsync("WO-1");

        await Should.NotThrowAsync(async () => await script.DisposeAsync());
    }

    [Test]
    public async Task ShouldSurviveDispose_WhenCircuitDisconnects()
    {
        var script = new ClipboardScript(new DisconnectingJsRuntime(failDispose: true));
        await script.CopyToClipboardAsync("WO-1");

        await Should.NotThrowAsync(async () => await script.DisposeAsync());
    }
}
