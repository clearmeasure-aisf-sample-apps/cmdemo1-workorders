using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class WaitingOnVendorToInProgressCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.InProgress,
            Assignee = employee
        };

        var command = new WaitingOnVendorToInProgressCommand(order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.WaitingOnVendor,
            Assignee = employee
        };

        var command = new WaitingOnVendorToInProgressCommand(order, new Employee());
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValid()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.WaitingOnVendor,
            Assignee = employee
        };

        var command = new WaitingOnVendorToInProgressCommand(order, employee);
        Assert.That(command.IsValid(), Is.True);
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Number = "123",
            Status = WorkOrderStatus.WaitingOnVendor,
            Assignee = employee
        };

        var command = new WaitingOnVendorToInProgressCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.InProgress));
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new WaitingOnVendorToInProgressCommand(order, employee);
    }
}
