using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

[TestFixture]
public class NotificationsSummaryTests
{
    [Test]
    public void Constructor_ShouldExposeTheThreeNotificationLists()
    {
        var overdue = new WorkOrder[] { new() { Number = "WO-1" } };
        var assigned = new WorkOrder[] { new() { Number = "WO-2" } };
        var created = new WorkOrder[] { new() { Number = "WO-3" } };

        var summary = new NotificationsSummary(overdue, assigned, created);

        summary.OverdueAndDueToday.ShouldBe(overdue);
        summary.NewlyAssignedLast7Days.ShouldBe(assigned);
        summary.CreatedByMeStatusChangedLast7Days.ShouldBe(created);
    }
}
