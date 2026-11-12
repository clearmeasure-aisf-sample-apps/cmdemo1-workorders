using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class CopyWorkOrderNumberButtonTests
{
    private const string ClipboardModule = "./_content/ClearMeasure.Bootcamp.UI.Shared/js/clipboard.js";

    [Test]
    public async Task ShouldCopyTheNumberAndSayCopied_WhenClicked()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses();
        var clipboard = ctx.JSInterop.SetupModule(ClipboardModule);
        clipboard.Setup<object>("copyToClipboard", _ => true).SetResult(new object());
        var button = ctx.Render<CopyWorkOrderNumberButton>(parameters => parameters.Add(p => p.Number, "WO-42"));

        var clicking = button.Find($"[data-testid='{nameof(WorkOrderManage.Elements.CopyWorkOrderNumber)}']").ClickAsync(new());

        await button.WaitForAssertionAsync(() => button.Find("#copy-status").TextContent.ShouldContain("Copied"));
        await clicking;
        clipboard.VerifyInvoke("copyToClipboard").Arguments.ShouldBe(["WO-42"]);
    }

    [Test]
    public async Task ShouldCopyNothing_WhenThereIsNoNumber()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses();
        var button = ctx.Render<CopyWorkOrderNumberButton>(parameters => parameters.Add(p => p.Number, ""));

        await button.Find($"[data-testid='{nameof(WorkOrderManage.Elements.CopyWorkOrderNumber)}']").ClickAsync(new());

        ctx.JSInterop.Invocations.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldIncludeTheNumberInTheAccessibleName_WhenNumberIsSet()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses();
        var cut = ctx.Render<CopyWorkOrderNumberButton>(parameters => parameters.Add(p => p.Number, "WO-42"));

        cut.Find($"[data-testid='{nameof(WorkOrderManage.Elements.CopyWorkOrderNumber)}']")
            .GetAttribute("aria-label")
            .ShouldBe("Copy work order number WO-42");
    }

    [Test]
    public async Task ShouldUseTheDefaultAccessibleName_WhenNumberIsEmpty()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses();
        var cut = ctx.Render<CopyWorkOrderNumberButton>();

        cut.Find($"[data-testid='{nameof(WorkOrderManage.Elements.CopyWorkOrderNumber)}']")
            .GetAttribute("aria-label")
            .ShouldBe("Copy work order number");
    }
}
