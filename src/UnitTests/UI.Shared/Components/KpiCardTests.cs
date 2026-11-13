using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class KpiCardTests
{
    [Test]
    public async Task ShouldRenderButton_WhenNoHref_AndRaiseOnSelected()
    {
        await using var ctx = new BunitContext();
        var selected = 0;

        var component = ctx.Render<KpiCard>(p => p
            .Add(c => c.TestId, "CardId")
            .Add(c => c.Label, "Overdue")
            .Add(c => c.Count, 3)
            .Add(c => c.OnSelected, () => selected++));
        await component.Find("button.summary-card[data-testid='CardId']").ClickAsync(new());

        selected.ShouldBe(1);
        component.FindAll("a").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldRenderLink_WhenHrefSet()
    {
        await using var ctx = new BunitContext();

        var component = ctx.Render<KpiCard>(p => p
            .Add(c => c.TestId, "CardId")
            .Add(c => c.Label, "Draft")
            .Add(c => c.Count, 3)
            .Add(c => c.Href, "workorder/search?Status=Draft"));

        component.Find("a.summary-card[data-testid='CardId']").GetAttribute("href")
            .ShouldBe("workorder/search?Status=Draft");
        component.FindAll("button").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldPutValueTestIdOnValue()
    {
        await using var ctx = new BunitContext();

        var withId = ctx.Render<KpiCard>(p => p
            .Add(c => c.TestId, "CardId")
            .Add(c => c.ValueTestId, "ValueId")
            .Add(c => c.Label, "Draft")
            .Add(c => c.Count, 7));
        var withoutId = ctx.Render<KpiCard>(p => p
            .Add(c => c.TestId, "OtherId")
            .Add(c => c.Label, "Draft")
            .Add(c => c.Count, 7));

        withId.Find(".summary-card-value[data-testid='ValueId']").TextContent.ShouldBe("7");
        withoutId.Find(".summary-card-value").HasAttribute("data-testid").ShouldBeFalse();
    }

    [Test]
    public async Task ShouldShowDash_WhenCountNull()
    {
        await using var ctx = new BunitContext();

        var component = ctx.Render<KpiCard>(p => p
            .Add(c => c.TestId, "CardId")
            .Add(c => c.Label, "Open"));

        component.Find(".summary-card-value").TextContent.ShouldBe("–");
    }

    [Test]
    public async Task ShouldOmitNote_WhenNull()
    {
        await using var ctx = new BunitContext();

        var withoutNote = ctx.Render<KpiCard>(p => p
            .Add(c => c.TestId, "CardId")
            .Add(c => c.Label, "Open")
            .Add(c => c.Count, 1));
        var withNote = ctx.Render<KpiCard>(p => p
            .Add(c => c.TestId, "CardId")
            .Add(c => c.Label, "Open")
            .Add(c => c.Count, 1)
            .Add(c => c.Note, "+2 this week")
            .Add(c => c.NoteTone, "note--success"));

        withoutNote.FindAll(".summary-card-note").Count.ShouldBe(0);
        withNote.Find(".summary-card-note.note--success").TextContent.ShouldBe("+2 this week");
    }

    [Test]
    public async Task ShouldBuildAriaLabel()
    {
        await using var ctx = new BunitContext();

        var withoutNote = ctx.Render<KpiCard>(p => p
            .Add(c => c.TestId, "CardId")
            .Add(c => c.Label, "Draft")
            .Add(c => c.Count, 3)
            .Add(c => c.Href, "workorder/search?Status=Draft"));
        var withNote = ctx.Render<KpiCard>(p => p
            .Add(c => c.TestId, "CardId")
            .Add(c => c.Label, "Overdue")
            .Add(c => c.Count, 2)
            .Add(c => c.Note, "Needs attention"));

        withoutNote.Find(".summary-card").GetAttribute("aria-label").ShouldBe("Draft: 3. Show these work orders");
        withNote.Find(".summary-card").GetAttribute("aria-label")
            .ShouldBe("Overdue: 2. Needs attention. Show these work orders");
    }
}
