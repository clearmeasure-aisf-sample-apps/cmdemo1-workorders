using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderTests
{
    [Test]
    public void PropertiesShouldInitializeToProperDefaults()
    {
        var workOrder = new WorkOrder();
        Assert.That(workOrder.Id, Is.EqualTo(Guid.Empty));
        Assert.That(workOrder.Title, Is.EqualTo(string.Empty));
        Assert.That(workOrder.Description, Is.EqualTo(string.Empty));
        Assert.That(workOrder.Instructions, Is.EqualTo(string.Empty));
        Assert.That(workOrder.Status, Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(workOrder.Number, Is.EqualTo(null));
        Assert.That(workOrder.Creator, Is.EqualTo(null));
        Assert.That(workOrder.Assignee, Is.EqualTo(null));
        Assert.That(workOrder.DueDate, Is.Null);
        Assert.That(workOrder.WaitingOnVendorSince, Is.Null);
        Assert.That(workOrder.IsWaitingOnVendor, Is.False);
    }

    [Test]
    public void ShouldDefaultPriorityToMedium()
    {
        var workOrder = new WorkOrder();

        workOrder.Priority.ShouldBe(WorkOrderPriority.Medium);
        workOrder.Building.ShouldBeNull();
    }

    [Test]
    public void ToStringShouldReturnWoNumber()
    {
        var order = new WorkOrder() { Number = "456" };
        Assert.That(order.ToString(), Is.EqualTo("Work Order 456"));
    }

    [Test]
    public void PropertiesShouldGetAndSetValuesProperly()
    {
        var workOrder = new WorkOrder();
        var guid = Guid.NewGuid();
        var creator = new Employee();
        var assignee = new Employee();

        workOrder.Id = guid;
        workOrder.Title = "Title";
        workOrder.Description = "Description";
        workOrder.Instructions = "Bring ladder and safety gear";
        workOrder.Status = WorkOrderStatus.Complete;
        workOrder.Number = "Number";
        workOrder.Creator = creator;
        workOrder.Assignee = assignee;

        Assert.That(workOrder.Id, Is.EqualTo(guid));
        Assert.That(workOrder.Title, Is.EqualTo("Title"));
        Assert.That(workOrder.Description, Is.EqualTo("Description"));
        Assert.That(workOrder.Instructions, Is.EqualTo("Bring ladder and safety gear"));
        Assert.That(workOrder.Status, Is.EqualTo(WorkOrderStatus.Complete));
        Assert.That(workOrder.Number, Is.EqualTo("Number"));
        Assert.That(workOrder.Creator, Is.EqualTo(creator));
        Assert.That(workOrder.Assignee, Is.EqualTo(assignee));
    }

    [Test]
    public void ShouldShowFriendlyStatusValuesAsStrings()
    {
        var workOrder = new WorkOrder() { Status = WorkOrderStatus.Assigned };

        Assert.That(workOrder.FriendlyStatus, Is.EqualTo("Assigned"));
    }

    [Test]
    public void ShouldNotTruncateDescriptionAsValidatorHandlesIt()
    {
        var longText = new string('x', 4001);
        var order = new WorkOrder() { Description = longText };
        Assert.That(order.Description.Length, Is.EqualTo(4001));
    }

    [Test]
    public void ShouldTruncateTo4000CharactersOnInstructions()
    {
        var longText = new string('x', 4001);
        var order = new WorkOrder() { Instructions = longText };
        Assert.That(order.Instructions.Length, Is.EqualTo(4000));
    }

    [Test]
    public void ShouldReturnEmptyStringWhenInstructionsSetToNull()
    {
        var order = new WorkOrder() { Instructions = null };
        Assert.That(order.Instructions, Is.EqualTo(string.Empty));
    }

    [Test]
    public void InstructionsMaxLength_ShouldBe4000()
    {
        Assert.That(WorkOrder.InstructionsMaxLength, Is.EqualTo(4000));
    }

    [Test]
    public void ShouldChangeStatus()
    {
        var order = new WorkOrder() { Status = WorkOrderStatus.Draft };
        order.ChangeStatus(WorkOrderStatus.Assigned);
        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Assigned));
    }

    [Test]
    public void IsWaitingOnVendor_InProgressWithField_IsTrue()
    {
        var workOrder = new WorkOrder { Status = WorkOrderStatus.InProgress, WaitingOnVendorSince = DateTime.UtcNow };

        workOrder.IsWaitingOnVendor.ShouldBeTrue();
    }

    [TestCase("Draft")]
    [TestCase("Assigned")]
    [TestCase("Complete")]
    [TestCase("Cancelled")]
    public void IsWaitingOnVendor_NotInProgressEvenWithField_IsFalse(string statusKey)
    {
        var workOrder = new WorkOrder { Status = WorkOrderStatus.FromKey(statusKey), WaitingOnVendorSince = DateTime.UtcNow };

        workOrder.IsWaitingOnVendor.ShouldBeFalse();
    }

    [Test]
    public void IsWaitingOnVendor_InProgressWithoutField_IsFalse()
    {
        var workOrder = new WorkOrder { Status = WorkOrderStatus.InProgress };

        workOrder.IsWaitingOnVendor.ShouldBeFalse();
    }

    [Test]
    public void ChangeStatus_ToInProgress_KeepsWaitingOnVendorSince()
    {
        var since = new DateTime(2026, 10, 14, 9, 0, 0);
        var workOrder = new WorkOrder { Status = WorkOrderStatus.InProgress, WaitingOnVendorSince = since };

        workOrder.ChangeStatus(new Employee(), DateTime.UtcNow, WorkOrderStatus.InProgress);

        workOrder.WaitingOnVendorSince.ShouldBe(since);
    }

    [Test]
    public void ChangeStatus_OutOfInProgress_ClearsWaitingOnVendorSince()
    {
        var workOrder = new WorkOrder { Status = WorkOrderStatus.InProgress, WaitingOnVendorSince = DateTime.UtcNow };

        workOrder.ChangeStatus(new Employee(), DateTime.UtcNow, WorkOrderStatus.Complete);

        workOrder.WaitingOnVendorSince.ShouldBeNull();
    }

    [Test]
    public void IsOverdue_WithNoDueDate_IsFalse()
    {
        var workOrder = new WorkOrder { Status = WorkOrderStatus.Assigned };

        workOrder.IsOverdue(new DateTime(2026, 10, 21)).ShouldBeFalse();
    }

    [Test]
    public void IsOverdue_WithFutureDueDate_IsFalse()
    {
        var workOrder = new WorkOrder { DueDate = new DateOnly(2026, 10, 22), Status = WorkOrderStatus.Assigned };

        workOrder.IsOverdue(new DateTime(2026, 10, 21)).ShouldBeFalse();
    }

    [Test]
    public void IsOverdue_WithPastDueDateAndOpenStatus_IsTrue()
    {
        var workOrder = new WorkOrder { DueDate = new DateOnly(2026, 10, 20), Status = WorkOrderStatus.Assigned };

        workOrder.IsOverdue(new DateTime(2026, 10, 21)).ShouldBeTrue();
    }

    [Test]
    public void IsOverdue_WithPastDueDateAndCompleteStatus_IsFalse()
    {
        var workOrder = new WorkOrder { DueDate = new DateOnly(2026, 10, 20), Status = WorkOrderStatus.Complete };

        workOrder.IsOverdue(new DateTime(2026, 10, 21)).ShouldBeFalse();
    }

    [Test]
    public void IsOverdue_WithPastDueDateAndCancelledStatus_IsFalse()
    {
        var workOrder = new WorkOrder { DueDate = new DateOnly(2026, 10, 20), Status = WorkOrderStatus.Cancelled };

        workOrder.IsOverdue(new DateTime(2026, 10, 21)).ShouldBeFalse();
    }
}
