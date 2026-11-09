using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

[TestFixture]
public class UserWorkOrdersTests
{
    private static readonly DateOnly Today = new(2026, 11, 9);
    private static readonly Employee Me = new("jsmith", "Jane", "Smith", "jane@example.com") { Id = Guid.NewGuid() };
    private static readonly Employee Other = new("bwayne", "Bruce", "Wayne", "bruce@example.com") { Id = Guid.NewGuid() };

    [Test]
    public void ShouldSliceTheWorkOrdersOfOneEmployee_WhenAssignedOrCreated()
    {
        var mine = new UserWorkOrders(Orders(), Me.Id, Today);

        mine.AssignedToMe.Select(wo => wo.Number).ShouldBe(["1", "2", "3"]);
        mine.OpenAssignedToMe.Select(wo => wo.Number).ShouldBe(["1", "2"]);
        mine.OpenAssignedToMeDueToday.Select(wo => wo.Number).ShouldBe(["2"]);
        mine.CreatedByMe.Select(wo => wo.Number).ShouldBe(["4"]);
    }

    private static IQueryable<WorkOrder> Orders() => new[]
    {
        new WorkOrder { Number = "1", Assignee = Me, Creator = Other, Status = WorkOrderStatus.Assigned, DueDate = Today.AddDays(3) },
        new WorkOrder { Number = "2", Assignee = Me, Creator = Other, Status = WorkOrderStatus.InProgress, DueDate = Today },
        new WorkOrder { Number = "3", Assignee = Me, Creator = Other, Status = WorkOrderStatus.Complete, DueDate = Today },
        new WorkOrder { Number = "4", Assignee = Other, Creator = Me, Status = WorkOrderStatus.Draft }
    }.AsQueryable();
}
