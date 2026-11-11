using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class WorkOrderStatusHistoryTests
{
    [Test]
    public async Task ShouldSayNothingWasRecorded_WhenThereAreNoChanges()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses();

        var history = ctx.Render<WorkOrderStatusHistory>(parameters => parameters.Add(p => p.Changes, []));

        history.Markup.ShouldContain("No status changes recorded.");
    }

    [Test]
    public async Task ShouldListEachChangeWithItsReason_WhenChangesExist()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses();
        var change = new WorkOrderStatusChange
        {
            FromStatus = WorkOrderStatus.InProgress,
            ToStatus = WorkOrderStatus.Assigned,
            ChangedDate = new DateTime(2026, 11, 9, 15, 0, 0, DateTimeKind.Utc),
            Reason = "Waiting on parts"
        };

        var history = ctx.Render<WorkOrderStatusHistory>(parameters => parameters.Add(p => p.Changes, [change]));

        history.FindAll($"[data-testid='{nameof(WorkOrderManage.Elements.StatusHistoryRow)}']").Count.ShouldBe(1);
        history.Find($"[data-testid='{nameof(WorkOrderManage.Elements.StatusHistoryReason)}']").TextContent.ShouldBe("Waiting on parts");
    }

    [Test]
    public async Task ShouldNameTableAndScopeColumnHeaders_WhenChangesExist()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses();
        var change = new WorkOrderStatusChange
        {
            FromStatus = WorkOrderStatus.Draft,
            ToStatus = WorkOrderStatus.Assigned,
            ChangedDate = new DateTime(2026, 11, 9, 15, 0, 0, DateTimeKind.Utc),
            Reason = "Assigned to tech"
        };

        var history = ctx.Render<WorkOrderStatusHistory>(parameters => parameters.Add(p => p.Changes, [change]));

        var title = history.Find("h2");
        title.TextContent.ShouldBe("Status Change History");
        history.Find("table").GetAttribute("aria-labelledby").ShouldBe(title.Id);
        history.FindAll("th").ShouldAllBe(th => th.GetAttribute("scope") == "col");
        history.FindAll("th").Count.ShouldBe(5);
    }
}
