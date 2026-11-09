using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class WorkOrderStatusTransitionTests
{
    private static readonly DateTime ChangedAt = new(2026, 11, 9, 15, 0, 0, DateTimeKind.Utc);

    [Test]
    public void ShouldRecordTheChange_WhenTheStatusMoved()
    {
        var fulfiller = new Employee("gwillie", "Groundskeeper", "Willie", "willie@example.com");
        var workOrder = new WorkOrder { Id = Guid.NewGuid(), Status = WorkOrderStatus.InProgress, Assignee = fulfiller };
        var transition = WorkOrderStatusTransition.Of(new AssignedToInProgressCommand(workOrder, fulfiller), WorkOrderStatus.Assigned, ChangedAt);

        var change = transition.ChangeOf(workOrder);

        change.ShouldNotBeNull();
        change.WorkOrderId.ShouldBe(workOrder.Id);
        change.FromStatus.ShouldBe(WorkOrderStatus.Assigned);
        change.ToStatus.ShouldBe(WorkOrderStatus.InProgress);
        change.ChangedById.ShouldBe(fulfiller.Id);
        change.ChangedDate.ShouldBe(ChangedAt);
        change.Reason.ShouldBeNull();
    }

    [Test]
    public void ShouldRecordNothing_WhenTheStatusDidNotMove()
    {
        var creator = new Employee("jsmith", "Jane", "Smith", "jane@example.com");
        var workOrder = new WorkOrder { Status = WorkOrderStatus.Draft, Creator = creator };
        var transition = WorkOrderStatusTransition.Of(new SaveDraftCommand(workOrder, creator), WorkOrderStatus.Draft, ChangedAt);

        var change = transition.ChangeOf(workOrder);

        change.ShouldBeNull();
    }

    [TestCase("  Waiting on parts  ", "Waiting on parts")]
    [TestCase("   ", null)]
    public void ShouldKeepTheTrimmedShelveReason_WhenAShelveGivesOne(string reason, string? expected)
    {
        var fulfiller = new Employee("gwillie", "Groundskeeper", "Willie", "willie@example.com");
        var workOrder = new WorkOrder { Status = WorkOrderStatus.Assigned, Assignee = fulfiller };
        var shelve = new InProgressToAssignedCommand(workOrder, fulfiller) { Reason = reason };

        var transition = WorkOrderStatusTransition.Of(shelve, WorkOrderStatus.InProgress, ChangedAt);

        transition.Reason.ShouldBe(expected);
    }
}
