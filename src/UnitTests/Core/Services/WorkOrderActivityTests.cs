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

        activity.ShouldBe(new WorkOrderActivity(WorkOrderActivityKind.Completed, Day.AddDays(2)));
    }

    [Test]
    public void Latest_ReturnsAssigned_WhenAssignedIsNewest()
    {
        var order = new WorkOrder { CreatedDate = Day, AssignedDate = Day.AddDays(3), CompletedDate = null };

        var activity = WorkOrderActivity.Latest(order);

        activity.ShouldBe(new WorkOrderActivity(WorkOrderActivityKind.Assigned, Day.AddDays(3)));
    }

    [TestCase(true, true, false, "Completed")]
    [TestCase(false, true, true, "Assigned")]
    [TestCase(true, false, true, "Completed")]
    [TestCase(true, true, true, "Completed")]
    public void Latest_PrefersCompletedThenAssigned_OnEqualDates(bool completed, bool assigned, bool created,
        string expectedDisplayName)
    {
        var order = new WorkOrder
        {
            CompletedDate = completed ? Day : null,
            AssignedDate = assigned ? Day : null,
            CreatedDate = created ? Day : null
        };

        var activity = WorkOrderActivity.Latest(order);

        activity.ShouldNotBeNull();
        WorkOrderActivityKindText.DisplayName(activity.Kind).ShouldBe(expectedDisplayName);
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

        activity.ShouldBe(new WorkOrderActivity(WorkOrderActivityKind.Created, Day));
    }

    [Test]
    public void ChurchDate_ConvertsUnspecifiedKindAsUtc()
    {
        var activity = new WorkOrderActivity(WorkOrderActivityKind.Completed,
            new DateTime(2026, 10, 14, 3, 0, 0, DateTimeKind.Unspecified));

        var churchDate = activity.ChurchDate;

        churchDate.ShouldBe(new DateOnly(2026, 10, 13));
    }

    [TestCase(WorkOrderActivityKind.Created, "Created")]
    [TestCase(WorkOrderActivityKind.Assigned, "Assigned")]
    [TestCase(WorkOrderActivityKind.Completed, "Completed")]
    public void DisplayName_ReturnsPersonFacingEventName(WorkOrderActivityKind kind, string expected)
    {
        WorkOrderActivityKindText.DisplayName(kind).ShouldBe(expected);
    }

    [Test]
    public void Kind_ValuesAreOrderedByPrecedence_LowestFirst()
    {
        // The tie-break is the enum itself: Completed over Assigned, Assigned over Created.
        ((int)WorkOrderActivityKind.Created).ShouldBeLessThan((int)WorkOrderActivityKind.Assigned);
        ((int)WorkOrderActivityKind.Assigned).ShouldBeLessThan((int)WorkOrderActivityKind.Completed);
    }
}
