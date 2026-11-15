using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.JSInterop;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class NotificationsPanelScriptTests
{
    [Test]
    public async Task ShouldSetupEscapeHandler_WhenOpened()
    {
        await using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(NotificationsPanelScript.ModulePath);
        module.SetupVoid("setupEscapeHandler", _ => true).SetVoidResult();
        var script = new NotificationsPanelScript(ctx.JSInterop.JSRuntime);
        using var dotNet = DotNetObjectReference.Create(new NotificationsPanel());

        await script.SetupEscapeHandlerAsync(dotNet);

        module.VerifyInvoke("setupEscapeHandler");
    }
}
