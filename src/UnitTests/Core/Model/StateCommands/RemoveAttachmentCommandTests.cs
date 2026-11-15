using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class RemoveAttachmentCommandTests
{
    private const string ClosedMessage = "Attachments cannot be removed from a Complete or Cancelled work order.";

    private readonly Employee _creator = EmployeeBuilder.Create("jpalermo")
        .WithFirstName("Jeffrey")
        .WithLastName("Palermo")
        .WithEmail("jp@example.com")
        .WithId(Guid.NewGuid())
        .Build();

    private readonly Employee _assignee = EmployeeBuilder.Create("tlovejoy")
        .WithFirstName("Timothy")
        .WithLastName("Lovejoy")
        .WithEmail("tl@example.com")
        .WithId(Guid.NewGuid())
        .Build();

    private readonly Employee _stranger = EmployeeBuilder.Create("hsimpson")
        .WithFirstName("Homer")
        .WithLastName("Simpson")
        .WithEmail("hs@example.com")
        .WithId(Guid.NewGuid())
        .Build();

    private WorkOrder NewWorkOrder(WorkOrderStatus status) =>
        WorkOrderBuilder.Create()
            .WithId(Guid.NewGuid())
            .WithNumber("WO-1")
            .CreatedBy(_creator)
            .AssignedTo(_assignee)
            .WithStatus(status)
            .Build();

    private static WorkOrderAttachment AttachmentOf(WorkOrder workOrder) =>
        new() { Id = Guid.NewGuid(), WorkOrderId = workOrder.Id, FileName = "photo.jpg" };

    [TestCase("Draft")]
    [TestCase("Assigned")]
    [TestCase("InProgress")]
    public void CanRemove_ShouldBeTrue_ForCreator(string statusKey)
    {
        var workOrder = NewWorkOrder(WorkOrderStatus.FromKey(statusKey));

        RemoveAttachmentCommand.CanRemove(workOrder, _creator).ShouldBeTrue();
    }

    [TestCase("Draft")]
    [TestCase("Assigned")]
    [TestCase("InProgress")]
    public void CanRemove_ShouldBeTrue_ForAssignee(string statusKey)
    {
        var workOrder = NewWorkOrder(WorkOrderStatus.FromKey(statusKey));

        RemoveAttachmentCommand.CanRemove(workOrder, _assignee).ShouldBeTrue();
    }

    [Test]
    public void CanRemove_ShouldBeTrue_WhenWaitingOnVendor()
    {
        var workOrder = NewWorkOrder(WorkOrderStatus.InProgress);
        workOrder.WaitingOnVendorSince = new DateTime(2025, 3, 1, 8, 0, 0);

        RemoveAttachmentCommand.CanRemove(workOrder, _creator).ShouldBeTrue();
    }

    [Test]
    public void CanRemove_ShouldBeFalse_ForUnrelatedEmployee()
    {
        var workOrder = NewWorkOrder(WorkOrderStatus.Draft);

        RemoveAttachmentCommand.CanRemove(workOrder, _stranger).ShouldBeFalse();
    }

    [TestCase("Complete")]
    [TestCase("Cancelled")]
    public void CanRemove_ShouldBeFalse_WhenClosed(string statusKey)
    {
        var workOrder = NewWorkOrder(WorkOrderStatus.FromKey(statusKey));

        RemoveAttachmentCommand.CanRemove(workOrder, _creator).ShouldBeFalse();
    }

    [Test]
    public void Validate_ShouldReturnNull_WhenRemovalAllowed()
    {
        var workOrder = NewWorkOrder(WorkOrderStatus.Draft);

        RemoveAttachmentCommand.Validate(workOrder, _creator, AttachmentOf(workOrder)).ShouldBeNull();
    }

    [Test]
    public void Validate_ShouldReturnWorkOrderNotFound_WhenWorkOrderMissing()
    {
        var workOrder = NewWorkOrder(WorkOrderStatus.Draft);

        RemoveAttachmentCommand.Validate(null, _creator, AttachmentOf(workOrder)).ShouldBe("Work order not found.");
    }

    [Test]
    public void Validate_ShouldReturnUserNotFound_WhenUserMissing()
    {
        var workOrder = NewWorkOrder(WorkOrderStatus.Draft);

        RemoveAttachmentCommand.Validate(workOrder, null, AttachmentOf(workOrder)).ShouldBe("User not found.");
    }

    [Test]
    public void Validate_ShouldReturnAttachmentNotFound_WhenAttachmentMissing()
    {
        var workOrder = NewWorkOrder(WorkOrderStatus.Draft);

        RemoveAttachmentCommand.Validate(workOrder, _creator, null).ShouldBe("Attachment not found.");
    }

    [Test]
    public void Validate_ShouldRejectAttachmentOfAnotherWorkOrder()
    {
        var workOrder = NewWorkOrder(WorkOrderStatus.Draft);
        var otherAttachment = AttachmentOf(NewWorkOrder(WorkOrderStatus.Draft));

        RemoveAttachmentCommand.Validate(workOrder, _creator, otherAttachment)
            .ShouldBe("Attachment does not belong to this work order.");
    }

    [Test]
    public void Validate_ShouldRejectUnrelatedEmployee()
    {
        var workOrder = NewWorkOrder(WorkOrderStatus.Draft);

        RemoveAttachmentCommand.Validate(workOrder, _stranger, AttachmentOf(workOrder))
            .ShouldBe("Only the creator or the assignee can remove attachments.");
    }

    [TestCase("Complete")]
    [TestCase("Cancelled")]
    public void Validate_ShouldRejectClosedWorkOrder(string statusKey)
    {
        var workOrder = NewWorkOrder(WorkOrderStatus.FromKey(statusKey));

        RemoveAttachmentCommand.Validate(workOrder, _creator, AttachmentOf(workOrder)).ShouldBe(ClosedMessage);
    }
}
