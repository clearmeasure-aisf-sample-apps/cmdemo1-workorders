using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

[TestFixture]
public class WorkOrderSpecificationsTests
{
    private static readonly TimeProvider Clock =
        FixedUtcClock.At(new DateTimeOffset(2026, 10, 14, 3, 0, 0, TimeSpan.Zero));

    private static readonly DateOnly Today = new(2026, 10, 13);

    [Test]
    public void ShouldMatchOpenStatuses_WhenOpenSpecificationApplied()
    {
        var orders = SampleOrders().AsQueryable();

        Numbers(orders.Where(WorkOrderSpecifications.Open)).ShouldBe(["1", "2", "3"]);
        SampleOrders().Where(WorkOrderSpecifications.IsOpen).Select(o => o.Number).ShouldBe(["1", "2", "3"]);
    }

    [TestCase("Draft", true)]
    [TestCase("Assigned", true)]
    [TestCase("InProgress", true)]
    [TestCase("Complete", false)]
    [TestCase("Cancelled", false)]
    public void ShouldMatchOpenExpression_WhenStatusCheckedDirectly(string statusKey, bool expected)
    {
        var status = WorkOrderStatus.FromKey(statusKey);

        WorkOrderSpecifications.IsOpen(status).ShouldBe(expected);
        WorkOrderSpecifications.Open.Compile()(new WorkOrder { Status = status }).ShouldBe(expected);
    }

    [Test]
    public void ShouldTreatNoneAsNotOpen_WhenStatusCheckedDirectly()
    {
        WorkOrderSpecifications.IsOpen(WorkOrderStatus.None).ShouldBeFalse();
        WorkOrderSpecifications.Open.Compile()(new WorkOrder { Status = WorkOrderStatus.None }).ShouldBeFalse();
    }

    [Test]
    public void ShouldMatchPastDueOpenOrders_WhenOverdueSpecificationApplied()
    {
        var orders = SampleOrders().AsQueryable();

        Numbers(orders.Where(WorkOrderSpecifications.Overdue(Today))).ShouldBe(["1"]);
    }

    [Test]
    public void ShouldMatchOpenOrdersDueThroughNextSixDays_WhenDueThisWeekApplied()
    {
        var orders = SampleOrders().AsQueryable();

        Numbers(orders.Where(WorkOrderSpecifications.DueThisWeek(Today))).ShouldBe(["2"]);
    }

    [Test]
    public void ShouldMatchCompletionsInsideWindow_WhenCompletedWithinApplied()
    {
        var orders = SampleOrders().AsQueryable();

        Numbers(orders.Where(WorkOrderSpecifications.CompletedWithin(30, Clock))).ShouldBe(["4"]);
    }

    private static WorkOrder[] SampleOrders()
    {
        var now = Clock.GetUtcNow().DateTime;
        return
        [
            new WorkOrder { Number = "1", Status = WorkOrderStatus.Draft, DueDate = Today.AddDays(-1) },
            new WorkOrder { Number = "2", Status = WorkOrderStatus.Assigned, DueDate = Today },
            new WorkOrder { Number = "3", Status = WorkOrderStatus.InProgress },
            new WorkOrder
            {
                Number = "4", Status = WorkOrderStatus.Complete, CompletedDate = now.AddDays(-30),
                DueDate = Today.AddDays(-9)
            },
            new WorkOrder
            {
                Number = "5", Status = WorkOrderStatus.Complete, CompletedDate = now.AddDays(-30).AddSeconds(-1)
            },
            new WorkOrder { Number = "6", Status = WorkOrderStatus.Cancelled, DueDate = Today.AddDays(-9) },
            new WorkOrder { Number = "7", Status = WorkOrderStatus.None, DueDate = Today.AddDays(-1) }
        ];
    }

    private static string?[] Numbers(IQueryable<WorkOrder> query) => query.Select(o => o.Number).ToArray();
}
