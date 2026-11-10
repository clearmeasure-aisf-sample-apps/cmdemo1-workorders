using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Services;

[TestFixture]
public class BulkStateChangeTests
{
    [Test]
    public void ShouldPrepareTheWorkOrderAndFindTheCommand_WhenTheVerbIsValid()
    {
        var creator = Employee("creator", canCreate: true, canFulfill: false);
        var fixer = Employee("fixer", canCreate: false, canFulfill: true);
        var draft = new WorkOrder { Number = "WO-1", Status = WorkOrderStatus.Draft, Creator = creator };
        var change = new BulkStateChange(DraftToAssignedCommand.Name, creator, order => order.Assignee = fixer);

        var command = change.CommandFor(draft);

        command.ShouldBeOfType<DraftToAssignedCommand>();
        draft.Assignee.ShouldBe(fixer);
    }

    [Test]
    public void ShouldFindNoCommand_WhenTheVerbDoesNotApplyToTheStatus()
    {
        var creator = Employee("creator", canCreate: true, canFulfill: false);
        var draft = new WorkOrder { Number = "WO-1", Status = WorkOrderStatus.Draft, Creator = creator };
        var change = new BulkStateChange("Complete", creator, _ => { });

        var command = change.CommandFor(draft);

        command.ShouldBeNull();
    }

    private static Employee Employee(string userName, bool canCreate, bool canFulfill)
    {
        var employee = new Employee(userName, userName, userName, $"{userName}@example.com");
        employee.AddRole(new Role("role", canCreate, canFulfill));
        return employee;
    }
}
