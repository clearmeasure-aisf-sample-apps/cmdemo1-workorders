using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class WorkOrderPriorityLabelTests
{
    [Test]
    public async Task ShouldNameThePriorityInItsStyle_WhenRendered()
    {
        await using var ctx = new BunitContext();
        var row = SearchResultRowMother.WithPriority(WorkOrderPriority.Low);

        var label = ctx.Render<WorkOrderPriorityLabel>(parameters => parameters.Add(p => p.Row, row));

        label.Find(".priority").TextContent.Trim().ShouldBe("Low");
        label.Find(".priority").ClassList.ShouldContain("priority--low");
    }

    [Test]
    public async Task ShouldShowTheHighBadge_WhenBadgesAreOnAndThePriorityIsHigh()
    {
        await using var ctx = new BunitContext();
        var row = SearchResultRowMother.WithPriority(WorkOrderPriority.High, "201");

        var label = ctx.Render<WorkOrderPriorityLabel>(parameters => parameters
            .Add(p => p.Row, row)
            .Add(p => p.ShowHighBadge, true));

        label.Find("[data-testid='PriorityBadge201']").TextContent.ShouldBe("High");
    }

    [Test]
    public async Task ShouldOmitTheHighBadge_WhenBadgesAreOff()
    {
        await using var ctx = new BunitContext();
        var row = SearchResultRowMother.WithPriority(WorkOrderPriority.High, "202");

        var label = ctx.Render<WorkOrderPriorityLabel>(parameters => parameters.Add(p => p.Row, row));

        label.FindAll("[data-testid='PriorityBadge202']").ShouldBeEmpty();
    }
}
