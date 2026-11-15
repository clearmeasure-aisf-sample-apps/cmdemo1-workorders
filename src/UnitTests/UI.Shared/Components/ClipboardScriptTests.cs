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
        module.Setup<object>("copyToClipboard", _ => true).SetResult(new object());
        var script = new ClipboardScript(ctx.JSInterop.JSRuntime);

        var copied = await script.CopyToClipboardAsync("WO-1");

        copied.ShouldBeTrue();
        module.VerifyInvoke("copyToClipboard").Arguments.ShouldBe(["WO-1"]);
    }
}
