using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class DatedWorkOrderBatchTests
{
    private static readonly DateTime Now = new(2026, 11, 9, 15, 0, 0, DateTimeKind.Utc);

    [Test]
    public void ShouldCreateOneAssignedWorkOrderPerDueDate_WhenTheBatchIsCreated()
    {
        var creator = new Employee("jsmith", "Jane", "Smith", "jane@example.com");
        var assignee = new Employee("gwillie", "Groundskeeper", "Willie", "willie@example.com");
        var batch = new DatedWorkOrderBatch("Mow", "Mow the lawn", creator, assignee,
            [new DateOnly(2026, 11, 14), new DateOnly(2026, 11, 21)]);

        var workOrders = batch.CreateWorkOrders(Now, new StubNumberGenerator());

        workOrders.Select(workOrder => workOrder.Number).ShouldBe(["WO-1", "WO-2"]);
        workOrders.Select(workOrder => workOrder.DueDate).ShouldBe([new DateOnly(2026, 11, 14), new DateOnly(2026, 11, 21)]);
        workOrders.ShouldAllBe(workOrder => workOrder.Status == WorkOrderStatus.Assigned
                                            && workOrder.Creator == creator
                                            && workOrder.Assignee == assignee
                                            && workOrder.CreatedDate == Now
                                            && workOrder.AssignedDate == Now
                                            && workOrder.Title == "Mow");
    }

    private sealed class StubNumberGenerator : IWorkOrderNumberGenerator
    {
        private int _next;

        public string GenerateNumber() => $"WO-{++_next}";
    }
}
