using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderSearchResultRowTests
{
    [Test]
    public void ShouldMarkTheRowOverdue_WhenAnOpenWorkOrderIsPastItsDueDate()
    {
        var workOrder = new WorkOrder { Number = "1", Status = WorkOrderStatus.Assigned, DueDate = new DateOnly(2026, 1, 5) };

        var row = WorkOrderSearchResultRow.For(workOrder, new NoonOn(new DateTimeOffset(2026, 1, 9, 18, 0, 0, TimeSpan.Zero)));

        row.Urgency.ShouldBe(DueDateUrgency.Overdue);
        row.DueDateDisplay.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ShouldLeaveTheDueDateBlank_WhenTheWorkOrderHasNone()
    {
        var workOrder = new WorkOrder { Number = "2", Status = WorkOrderStatus.Draft };

        var row = WorkOrderSearchResultRow.For(workOrder, TimeProvider.System);

        row.Urgency.ShouldBe(DueDateUrgency.None);
        row.DueDateDisplay.ShouldBeNullOrEmpty();
    }

    private sealed class NoonOn(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
