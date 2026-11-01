using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class InProgressToAssignedCommandTests : StateCommandBaseTests
{
    [Test]
    public void Should_HaveInProgressBeginStatus_When_Created()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };
        var command = new InProgressToAssignedCommand(order, employee);

        command.GetBeginStatus().ShouldBe(WorkOrderStatus.InProgress);
    }

    [Test]
    public void Should_HaveAssignedEndStatus_When_Executed()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };
        var command = new InProgressToAssignedCommand(order, employee);

        var testContext = new StateCommandContext();
        command.Execute(testContext);

        order.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test]
    public void Should_NotBeValid_When_StatusIsNotInProgress()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.Draft, Assignee = employee };
        var command = new InProgressToAssignedCommand(order, employee);

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void Should_NotBeValid_When_CurrentUserIsNotAssignee()
    {
        var assignee = new Employee();
        var otherEmployee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = assignee };
        var command = new InProgressToAssignedCommand(order, otherEmployee);

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void Should_BeValid_When_StatusIsInProgressAndUserIsAssignee()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };
        var command = new InProgressToAssignedCommand(order, employee);

        command.IsValid().ShouldBeTrue();
    }

    [Test]
    public void Should_NotBeValid_When_AssigneeIsNull()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = null };
        var command = new InProgressToAssignedCommand(order, employee);

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void Should_ChangeStatusToAssigned_When_ExecuteIsCalledWithValidCommand()
    {
        var employee = new Employee();
        var order = new WorkOrder
        {
            Number = "WO-123",
            Status = WorkOrderStatus.InProgress,
            Assignee = employee
        };
        var command = new InProgressToAssignedCommand(order, employee);
        var context = new StateCommandContext();

        command.Execute(context);

        order.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test]
    public void Should_HaveCorrectTransitionVerbPresentTense()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };
        var command = new InProgressToAssignedCommand(order, employee);

        command.TransitionVerbPresentTense.ShouldBe("Shelve");
    }

    [Test]
    public void Should_HaveCorrectTransitionVerbPastTense()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };
        var command = new InProgressToAssignedCommand(order, employee);

        command.TransitionVerbPastTense.ShouldBe("Shelved");
    }

    [Test]
    public void Should_MatchCommandName()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };
        var command = new InProgressToAssignedCommand(order, employee);

        command.Matches("Shelve").ShouldBeTrue();
        command.Matches("InvalidCommand").ShouldBeFalse();
    }

    [TestCase("Draft")]
    [TestCase("Assigned")]
    [TestCase("Complete")]
    [TestCase("Cancelled")]
    public void Should_NotBeValid_When_StatusIsNotInProgress_TestCase(string statusKey)
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.FromKey(statusKey), Assignee = employee };
        var command = new InProgressToAssignedCommand(order, employee);

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void Should_ClearWaitingOnVendorSince_When_Shelved()
    {
        var employee = new Employee();
        var order = new WorkOrder
        {
            Number = "WO-123",
            Status = WorkOrderStatus.InProgress,
            Assignee = employee,
            WaitingOnVendorSince = DateTime.UtcNow
        };

        new InProgressToAssignedCommand(order, employee).Execute(new StateCommandContext());

        order.WaitingOnVendorSince.ShouldBeNull();
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new InProgressToAssignedCommand(order, employee);
    }
}
