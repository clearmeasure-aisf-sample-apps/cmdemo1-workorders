using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class InProgressToWaitingOnVendorCommandTests : StateCommandBaseTests
{
    [Test]
    public void Should_HaveInProgressBeginStatus_When_Created()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };
        var command = new InProgressToWaitingOnVendorCommand(order, employee);

        command.GetBeginStatus().ShouldBe(WorkOrderStatus.InProgress);
    }

    [Test]
    public void Should_BeValid_When_InProgressAssigneeAndNotWaiting()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };
        var command = new InProgressToWaitingOnVendorCommand(order, employee);

        command.IsValid().ShouldBeTrue();
    }

    [Test]
    public void Should_NotBeValid_When_CurrentUserIsNotAssignee()
    {
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = new Employee() };
        var command = new InProgressToWaitingOnVendorCommand(order, new Employee());

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void Should_NotBeValid_When_AssigneeIsNull()
    {
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = null };
        var command = new InProgressToWaitingOnVendorCommand(order, new Employee());

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void Should_NotBeValid_When_AlreadyWaitingOnVendor()
    {
        var employee = new Employee();
        var order = new WorkOrder
        {
            Status = WorkOrderStatus.InProgress,
            Assignee = employee,
            WaitingOnVendorSince = new DateTime(2026, 10, 13, 8, 0, 0)
        };
        var command = new InProgressToWaitingOnVendorCommand(order, employee);

        command.IsValid().ShouldBeFalse();
    }

    [TestCase("Draft")]
    [TestCase("Assigned")]
    [TestCase("Complete")]
    [TestCase("Cancelled")]
    public void Should_NotBeValid_When_StatusIsNotInProgress(string statusKey)
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.FromKey(statusKey), Assignee = employee };
        var command = new InProgressToWaitingOnVendorCommand(order, employee);

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void Should_SetWaitingOnVendorSinceToContextTimeAndStayInProgress_When_Executed()
    {
        var employee = new Employee();
        var order = new WorkOrder { Number = "WO-1", Status = WorkOrderStatus.InProgress, Assignee = employee };
        var command = new InProgressToWaitingOnVendorCommand(order, employee);
        var now = new DateTime(2026, 10, 14, 9, 0, 0);

        command.Execute(new StateCommandContext { CurrentDateTime = now });

        order.WaitingOnVendorSince.ShouldBe(now);
        order.Status.ShouldBe(WorkOrderStatus.InProgress);
        order.IsWaitingOnVendor.ShouldBeTrue();
    }

    [Test]
    public void Should_HaveCorrectTransitionVerbs()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };
        var command = new InProgressToWaitingOnVendorCommand(order, employee);

        command.TransitionVerbPresentTense.ShouldBe("Wait on vendor");
        command.TransitionVerbPastTense.ShouldBe("Waiting on vendor");
    }

    [Test]
    public void Should_MatchCommandName()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };
        var command = new InProgressToWaitingOnVendorCommand(order, employee);

        command.Matches(InProgressToWaitingOnVendorCommand.Name).ShouldBeTrue();
        command.Matches("InvalidCommand").ShouldBeFalse();
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new InProgressToWaitingOnVendorCommand(order, employee);
    }
}
