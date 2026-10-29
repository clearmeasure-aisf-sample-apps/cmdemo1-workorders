using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class WaitingOnVendorToCancelledCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.Draft,
            Creator = employee,
            WaitingOnVendorSince = DateTime.UtcNow
        };

        var command = new WaitingOnVendorToCancelledCommand(order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithoutWaitingOnVendorState()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.InProgress,
            Creator = employee,
            WaitingOnVendorSince = null
        };

        var command = new WaitingOnVendorToCancelledCommand(order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.InProgress,
            Creator = employee,
            WaitingOnVendorSince = DateTime.UtcNow
        };

        var command = new WaitingOnVendorToCancelledCommand(order, new Employee());
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValid()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.InProgress,
            Creator = employee,
            WaitingOnVendorSince = DateTime.UtcNow
        };

        var command = new WaitingOnVendorToCancelledCommand(order, employee);
        Assert.That(command.IsValid(), Is.True);
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Number = "123",
            Status = WorkOrderStatus.InProgress,
            Creator = employee,
            WaitingOnVendorSince = DateTime.UtcNow
        };

        var command = new WaitingOnVendorToCancelledCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Cancelled));
    }

    [Test]
    public void ShouldClearAssigneeWhenCancelling()
    {
        var creator = new Employee();
        var assignee = new Employee();
        var order = new WorkOrder()
        {
            Number = "456",
            Status = WorkOrderStatus.InProgress,
            Creator = creator,
            Assignee = assignee,
            AssignedDate = DateTime.Now,
            WaitingOnVendorSince = DateTime.UtcNow
        };

        var command = new WaitingOnVendorToCancelledCommand(order, creator);
        command.Execute(new StateCommandContext());

        Assert.That(order.Assignee, Is.Null);
        Assert.That(order.AssignedDate, Is.Null);
        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Cancelled));
    }

    [Test]
    public void ShouldNotBeValidWhenAssigneeTriesToCancel()
    {
        var creator = new Employee();
        var assignee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.InProgress,
            Creator = creator,
            Assignee = assignee,
            WaitingOnVendorSince = DateTime.UtcNow
        };

        var command = new WaitingOnVendorToCancelledCommand(order, assignee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldClearWaitingOnVendorSince_WhenCancelled()
    {
        var employee = new Employee();
        var order = new WorkOrder
        {
            Number = "123",
            Status = WorkOrderStatus.InProgress,
            Creator = employee,
            Assignee = employee,
            WaitingOnVendorSince = DateTime.UtcNow
        };

        new WaitingOnVendorToCancelledCommand(order, employee).Execute(new StateCommandContext());

        Assert.That(order.WaitingOnVendorSince, Is.Null);
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        order.WaitingOnVendorSince = DateTime.UtcNow;
        return new WaitingOnVendorToCancelledCommand(order, employee);
    }
}
