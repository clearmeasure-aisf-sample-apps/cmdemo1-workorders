using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class CounterPageTests
{
    [Test]
    public async Task Should_DisplaySubtitle_WithVolunteerServiceText()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Counter>();

        component.Find($"[data-testid='{nameof(Counter.Elements.CounterSubtitle)}']")
            .TextContent
            .ShouldBe("Track church maintenance activities and volunteer service");
    }

    [Test]
    public async Task Should_DisplayUsageHint_WithExactClickInstruction()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Counter>();

        component.Find($"[data-testid='{nameof(Counter.Elements.CounterHint)}']")
            .TextContent
            .ShouldBe("Click the button to increase the count.");
    }

    [Test]
    public async Task ShouldRenderTooltipWithExactTitleOnIncrementButton()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<Counter>();

        var button = component.Find($"[data-testid='{nameof(Counter.Elements.IncrementButton)}']");
        button.GetAttribute("title").ShouldBe("Increase the count by one");
    }

    [Test]
    public async Task Should_DecrementCount_WhenRemoveActivityClickedAfterIncrements()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<Counter>();

        var increment = component.Find($"[data-testid='{nameof(Counter.Elements.IncrementButton)}']");
        await increment.ClickAsync(new());
        await increment.ClickAsync(new());
        await increment.ClickAsync(new());

        await component.Find($"[data-testid='{nameof(Counter.Elements.DecrementButton)}']").ClickAsync(new());

        component.Find($"[data-testid='{nameof(Counter.Elements.CounterValue)}']")
            .TextContent
            .ShouldBe("2");
    }

    [Test]
    public async Task Should_DisableDecrementButton_WhenCountIsZero()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<Counter>();

        var decrement = component.Find($"[data-testid='{nameof(Counter.Elements.DecrementButton)}']");
        decrement.HasAttribute("disabled").ShouldBeTrue();

        await decrement.ClickAsync(new());

        component.Find($"[data-testid='{nameof(Counter.Elements.CounterValue)}']")
            .TextContent
            .ShouldBe("0");
    }

    [Test]
    public async Task Should_DisableDecrementButton_AfterReset()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<Counter>();

        await component.Find($"[data-testid='{nameof(Counter.Elements.IncrementButton)}']").ClickAsync(new());
        await component.Find($"[data-testid='{nameof(Counter.Elements.ResetButton)}']").ClickAsync(new());

        component.Find($"[data-testid='{nameof(Counter.Elements.DecrementButton)}']")
            .HasAttribute("disabled")
            .ShouldBeTrue();
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        return ctx;
    }
}
