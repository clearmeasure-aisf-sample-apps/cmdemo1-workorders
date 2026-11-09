using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class WorkOrderActivityTests
{
    private static readonly DateTime Day = new(2026, 10, 1, 12, 0, 0);

    [Test]
    public void Latest_ReturnsCompleted_WhenCompletedIsNewest()
    {
        var order = new WorkOrder { CreatedDate = Day, AssignedDate = Day.AddDays(1), CompletedDate = Day.AddDays(2) };

        var activity = WorkOrderActivity.Latest(order);

        activity.ShouldBe(new WorkOrderActivity("Completed", Day.AddDays(2)));
    }

    [Test]
    public void Latest_ReturnsAssigned_WhenAssignedIsNewest()
    {
        var order = new WorkOrder { CreatedDate = Day, AssignedDate = Day.AddDays(3), CompletedDate = null };

        var activity = WorkOrderActivity.Latest(order);

        activity.ShouldBe(new WorkOrderActivity("Assigned", Day.AddDays(3)));
    }

    [TestCase(true, true, false, "Completed")]
    [TestCase(false, true, true, "Assigned")]
    [TestCase(true, false, true, "Completed")]
    [TestCase(true, true, true, "Completed")]
    public void Latest_PrefersCompletedThenAssigned_OnEqualDates(bool completed, bool assigned, bool created,
        string expectedVerb)
    {
        var order = new WorkOrder
        {
            CompletedDate = completed ? Day : null,
            AssignedDate = assigned ? Day : null,
            CreatedDate = created ? Day : null
        };

        var activity = WorkOrderActivity.Latest(order);

        activity.ShouldNotBeNull();
        activity.Verb.ShouldBe(expectedVerb);
        activity.WhenUtc.ShouldBe(Day);
    }

    [Test]
    public void Latest_ReturnsNull_WhenNoDates()
    {
        var order = new WorkOrder { CreatedDate = null, AssignedDate = null, CompletedDate = null };

        var activity = WorkOrderActivity.Latest(order);

        activity.ShouldBeNull();
    }

    [Test]
    public void Latest_ReturnsCreated_ForCancelledOrderWithNullAssignedDate()
    {
        var order = new WorkOrder
        {
            Status = WorkOrderStatus.Cancelled, CreatedDate = Day, AssignedDate = null, CompletedDate = null
        };

        var activity = WorkOrderActivity.Latest(order);

        activity.ShouldBe(new WorkOrderActivity("Created", Day));
    }

    [Test]
    public void ChurchDate_ConvertsUnspecifiedKindAsUtc()
    {
        var activity = new WorkOrderActivity("Completed",
            new DateTime(2026, 10, 14, 3, 0, 0, DateTimeKind.Unspecified));

        var churchDate = activity.ChurchDate;

        churchDate.ShouldBe(new DateOnly(2026, 10, 13));
    }
}
