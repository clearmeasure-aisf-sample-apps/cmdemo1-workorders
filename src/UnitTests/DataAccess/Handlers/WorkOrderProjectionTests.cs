using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderProjectionTests
{
    private static readonly TimeProvider Clock =
        FixedUtcClock.At(new DateTimeOffset(2026, 10, 13, 18, 0, 0, TimeSpan.Zero));

    private static readonly DateOnly Today = new(2026, 10, 13);

    [Test]
    public void ShouldProjectKeysAndFlags_WhenCountRowApplied()
    {
        var orders = CountRowSamples().AsQueryable();

        var rows = orders.Select(WorkOrderProjection.CountRow(Today, Clock, 30)).ToArray();

        rows.Select(FlagTuple).ShouldBe(ExpectedCountRowFlags());
    }

    [Test]
    public void ShouldProjectAssigneeLoadFlags_WhenAssigneePresent()
    {
        var assignee = new Employee("a", "A", "A", "a@test.com");
        var orders = new[]
        {
            new WorkOrder
            {
                Number = "1", Assignee = assignee, Status = WorkOrderStatus.InProgress, DueDate = Today.AddDays(-1)
            }
        }.AsQueryable();

        var flags = orders.Select(WorkOrderProjection.AssigneeLoadFlags(Today, Clock, 30)).Single();

        flags.AssigneeId.ShouldBe(assignee.Id);
        flags.Open.ShouldBe(1);
        flags.InProgress.ShouldBe(1);
        flags.Overdue.ShouldBe(1);
        flags.Completed.ShouldBe(0);
    }

    private static WorkOrder[] CountRowSamples()
    {
        var now = Clock.GetUtcNow().DateTime;
        return
        [
            new WorkOrder
            {
                Number = "1", Building = " Hall ", RoomNumber = "101 ", Status = WorkOrderStatus.Assigned,
                DueDate = Today.AddDays(-1)
            },
            new WorkOrder
            {
                Number = "2", Building = null, RoomNumber = "  ", Status = WorkOrderStatus.Complete,
                CompletedDate = now.AddDays(-1)
            },
            new WorkOrder
            {
                Number = "3", Building = "", RoomNumber = null, Status = WorkOrderStatus.Cancelled,
                DueDate = Today.AddDays(-1)
            }
        ];
    }

    private static (string? Number, string Building, string Room, int Open, int Overdue, int Completed) FlagTuple(
        WorkOrderCountRow row) =>
        (row.Number, row.Building, row.Room, row.Open, row.Overdue, row.Completed);

    private static (string?, string, string, int, int, int)[] ExpectedCountRowFlags() =>
    [
        ("1", "Hall", "101", 1, 1, 0),
        ("2", "", "", 0, 0, 1),
        ("3", "", "", 0, 0, 0)
    ];
}
