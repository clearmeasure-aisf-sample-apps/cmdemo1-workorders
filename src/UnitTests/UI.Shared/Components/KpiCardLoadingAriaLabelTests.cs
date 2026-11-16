using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class KpiCardLoadingAriaLabelTests
{
    [Test]
    public async Task ShouldShowLoadingInAriaLabel_WhenCountIsNull()
    {
        await using var ctx = new BunitContext();

        var component = ctx.Render<KpiCard>(p => p
            .Add(c => c.TestId, "CardId")
            .Add(c => c.Label, "Open work orders")
            .Add(c => c.Count, (int?)null));

        component.Find(".summary-card").GetAttribute("aria-label")
            .ShouldBe("Open work orders: loading. Show these work orders");
        component.Find(".summary-card-value").TextContent.ShouldBe("–");
    }

    [Test]
    public async Task ShouldShowLoadingInAriaLabel_WhenCountIsNullWithNote()
    {
        await using var ctx = new BunitContext();

        var component = ctx.Render<KpiCard>(p => p
            .Add(c => c.TestId, "CardId")
            .Add(c => c.Label, "Overdue")
            .Add(c => c.Count, (int?)null)
            .Add(c => c.Note, "Needs attention"));

        component.Find(".summary-card").GetAttribute("aria-label")
            .ShouldBe("Overdue: loading. Needs attention. Show these work orders");
        component.Find(".summary-card-value").TextContent.ShouldBe("–");
    }

    [Test]
    public async Task ShouldShowCountInAriaLabel_WhenCountIsNotNull()
    {
        await using var ctx = new BunitContext();

        var component = ctx.Render<KpiCard>(p => p
            .Add(c => c.TestId, "CardId")
            .Add(c => c.Label, "Draft")
            .Add(c => c.Count, 3));

        component.Find(".summary-card").GetAttribute("aria-label")
            .ShouldBe("Draft: 3. Show these work orders");
        component.Find(".summary-card-value").TextContent.ShouldBe("3");
    }
}
