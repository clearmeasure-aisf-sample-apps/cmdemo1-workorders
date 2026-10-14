using Bunit;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class WorkOrderSummaryCardsTests
{
    [Test]
    public async Task ShouldRenderFourCardsWithCountsAndNotes()
    {
        await using var ctx = new BunitContext();
        var summary = new WorkOrderSearchSummary
        {
            OpenCount = 24, OpenedLast7Days = 3, AssignedToMeCount = 2, AssignedToMeDueToday = 1,
            OverdueCount = 3, CompletedLast30Days = 41, AverageDaysToClose = 2.44
        };

        var component = ctx.Render<WorkOrderSummaryCards>(p => p.Add(c => c.Summary, summary));

        var cards = component.FindAll("button.summary-card");
        cards.Count.ShouldBe(4);
        Text(component, "SummaryCardOpen").ShouldBe("Open work orders 24 +3 this week");
        Text(component, "SummaryCardAssignedToMe").ShouldBe("Assigned to me 2 1 due today");
        Text(component, "SummaryCardOverdue").ShouldBe("Overdue 3 Needs attention");
        Text(component, "SummaryCardCompleted").ShouldBe("Completed (30 days) 41 Avg. 2.4 days to close");
    }

    [Test]
    public async Task ShouldRenderDashesAndNoAverage_WhenSummaryMissing()
    {
        await using var ctx = new BunitContext();

        var component = ctx.Render<WorkOrderSummaryCards>();

        component.FindAll(".summary-card-value").ShouldAllBe(v => v.TextContent == "–");
        Text(component, "SummaryCardCompleted").ShouldEndWith("No recent closures");
    }

    [Test]
    public async Task ShouldRaiseCardSelected_WhenCardClicked()
    {
        await using var ctx = new BunitContext();
        SummaryCard? selected = null;
        var component = ctx.Render<WorkOrderSummaryCards>(p => p
            .Add(c => c.Summary, new WorkOrderSearchSummary())
            .Add(c => c.OnCardSelected, card => selected = card));

        await component.Find("[data-testid='SummaryCardCompleted']").ClickAsync(new());

        selected.ShouldBe(SummaryCard.Completed);
        component.Find("[data-testid='SummaryCardOverdue']").GetAttribute("aria-label")!.ShouldContain("Overdue: 0");
    }

    [Test]
    public async Task ShouldRenderLinks_WhenLinkToSearch()
    {
        await using var ctx = new BunitContext();

        var component = ctx.Render<WorkOrderSummaryCards>(p => p
            .Add(c => c.Summary, new WorkOrderSearchSummary())
            .Add(c => c.LinkToSearch, true));

        component.FindAll("a.summary-card").Count.ShouldBe(4);
        component.FindAll("button.summary-card").Count.ShouldBe(0);
        component.Find("[data-testid='SummaryCardAssignedToMe']").GetAttribute("href")
            .ShouldBe("workorder/search?AssignedToMe=true&OpenOnly=true");
        Href(component, "SummaryCardOpen").ShouldBe(WorkOrderSearchUrlState.ForCard(SummaryCard.Open).ToRelativeUri());
        Href(component, "SummaryCardAssignedToMe").ShouldBe(WorkOrderSearchUrlState.ForCard(SummaryCard.AssignedToMe).ToRelativeUri());
        Href(component, "SummaryCardOverdue").ShouldBe(WorkOrderSearchUrlState.ForCard(SummaryCard.Overdue).ToRelativeUri());
        Href(component, "SummaryCardCompleted").ShouldBe(WorkOrderSearchUrlState.ForCard(SummaryCard.Completed).ToRelativeUri());
    }

    private static string? Href(IRenderedComponent<WorkOrderSummaryCards> component, string testId) =>
        component.Find($"[data-testid='{testId}']").GetAttribute("href");

    private static string Text(IRenderedComponent<WorkOrderSummaryCards> component, string testId) =>
        System.Text.RegularExpressions.Regex.Replace(component.Find($"[data-testid='{testId}']").TextContent, @"\s+", " ").Trim();
}
