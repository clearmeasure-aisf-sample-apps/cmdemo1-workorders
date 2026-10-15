using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class WaitingOnVendorToInProgressCommandTests : StateCommandBaseTests
{
    private static readonly DateTime WaitingSince = new(2026, 10, 13, 8, 0, 0);

    private static WorkOrder WaitingOrder(Employee assignee) => new()
    {
        Number = "WO-1",
        Status = WorkOrderStatus.InProgress,
        Assignee = assignee,
        WaitingOnVendorSince = WaitingSince
    };

    [Test]
    public void Should_HaveInProgressBeginStatus_When_Created()
    {
        var employee = new Employee();
        var command = new WaitingOnVendorToInProgressCommand(WaitingOrder(employee), employee);

        command.GetBeginStatus().ShouldBe(WorkOrderStatus.InProgress);
    }

    [Test]
    public void Should_BeValid_When_InProgressAssigneeAndWaiting()
    {
        var employee = new Employee();
        var command = new WaitingOnVendorToInProgressCommand(WaitingOrder(employee), employee);

        command.IsValid().ShouldBeTrue();
    }

    [Test]
    public void Should_NotBeValid_When_NotWaitingOnVendor()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };
        var command = new WaitingOnVendorToInProgressCommand(order, employee);

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void Should_NotBeValid_When_CurrentUserIsNotAssignee()
    {
        var command = new WaitingOnVendorToInProgressCommand(WaitingOrder(new Employee()), new Employee());

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void Should_NotBeValid_When_AssigneeIsNull()
    {
        var order = WaitingOrder(new Employee());
        order.Assignee = null;
        var command = new WaitingOnVendorToInProgressCommand(order, new Employee());

        command.IsValid().ShouldBeFalse();
    }

    [TestCase("Draft")]
    [TestCase("Assigned")]
    [TestCase("Complete")]
    [TestCase("Cancelled")]
    public void Should_NotBeValid_When_StatusIsNotInProgress(string statusKey)
    {
        var employee = new Employee();
        var order = WaitingOrder(employee);
        order.Status = WorkOrderStatus.FromKey(statusKey);
        var command = new WaitingOnVendorToInProgressCommand(order, employee);

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void Should_ClearWaitingOnVendorSinceAndStayInProgress_When_Executed()
    {
        var employee = new Employee();
        var order = WaitingOrder(employee);
        var command = new WaitingOnVendorToInProgressCommand(order, employee);

        command.Execute(new StateCommandContext { CurrentDateTime = new DateTime(2026, 10, 14, 9, 0, 0) });

        order.WaitingOnVendorSince.ShouldBeNull();
        order.Status.ShouldBe(WorkOrderStatus.InProgress);
        order.IsWaitingOnVendor.ShouldBeFalse();
    }

    [Test]
    public void Should_HaveCorrectTransitionVerbs()
    {
        var employee = new Employee();
        var command = new WaitingOnVendorToInProgressCommand(WaitingOrder(employee), employee);

        command.TransitionVerbPresentTense.ShouldBe("Resume");
        command.TransitionVerbPastTense.ShouldBe("Resumed");
    }

    [Test]
    public void Should_MatchCommandName()
    {
        var employee = new Employee();
        var command = new WaitingOnVendorToInProgressCommand(WaitingOrder(employee), employee);

        command.Matches(WaitingOnVendorToInProgressCommand.Name).ShouldBeTrue();
        command.Matches("InvalidCommand").ShouldBeFalse();
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new WaitingOnVendorToInProgressCommand(order, employee);
    }
}
