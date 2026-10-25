using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class SaveDraftCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.Complete,
            Creator = employee
        };

        var command = new SaveDraftCommand(order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.Draft,
            Creator = employee
        };

        var command = new SaveDraftCommand(order, new Employee());
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValid()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Status = WorkOrderStatus.Draft,
            Creator = employee
        };

        var command = new SaveDraftCommand(order, employee);
        Assert.That(command.IsValid(), Is.True);
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var employee = new Employee();
        var order = new WorkOrder()
        {
            Number = "123",
            Status = WorkOrderStatus.Draft,
            Creator = employee
        };

        var command = new SaveDraftCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(order.CreatedDate, Is.Not.Null);
    }

    [Test]
    public void WithPreviousRoomNumber_ShouldSetPreviousRoomNumber_WhenSaveDraftCommand()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.Draft, Creator = employee };
        var command = new SaveDraftCommand(order, employee);

        var result = SaveDraftCommand.WithPreviousRoomNumber(command, "Room A");

        var save = result as SaveDraftCommand;
        Assert.That(save, Is.Not.Null);
        Assert.That(save!.PreviousRoomNumber, Is.EqualTo("Room A"));
        Assert.That(save.WorkOrder, Is.SameAs(order));
        Assert.That(save.CurrentUser, Is.SameAs(employee));
    }

    [Test]
    public void WithPreviousRoomNumber_ShouldReturnSameInstance_WhenOtherCommand()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.Draft, Creator = employee };
        IStateCommand command = new DraftToAssignedCommand(order, employee);

        var result = SaveDraftCommand.WithPreviousRoomNumber(command, "Room A");

        Assert.That(result, Is.SameAs(command));
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new SaveDraftCommand(order, employee);
    }
}
