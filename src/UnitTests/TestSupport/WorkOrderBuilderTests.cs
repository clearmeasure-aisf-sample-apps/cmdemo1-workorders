using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.TestSupport;

[TestFixture]
public class WorkOrderBuilderTests
{
    [Test]
    public void ShouldBuildDraft_WithCreatorAndNumber()
    {
        var creator = EmployeeBuilder.Create("ned").WithFirstName("Ned").WithLastName("Flanders").Build();

        var order = WorkOrderBuilder.Create()
            .WithNumber("WO-1")
            .CreatedBy(creator)
            .WithStatus(WorkOrderStatus.Draft)
            .Build();

        order.Number.ShouldBe("WO-1");
        order.Title.ShouldBe("WO-1");
        order.Creator.ShouldBe(creator);
        order.Status.ShouldBe(WorkOrderStatus.Draft);
        order.Priority.ShouldBe(WorkOrderPriority.Medium);
    }

    [Test]
    public void ShouldAssignAndDate_WhenFluentStepsSetDomainFields()
    {
        var creator = EmployeeBuilder.Create("creator").Build();
        var assignee = EmployeeBuilder.Create("willie").Build();
        var due = new DateOnly(2026, 6, 15);
        var completed = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);

        var order = WorkOrderBuilder.Create()
            .CreatedBy(creator)
            .Assigned(assignee)
            .DueOn(due)
            .CompletedAt(completed)
            .InBuilding("Sanctuary")
            .InRoom("101")
            .WithPriority(WorkOrderPriority.High)
            .Build();

        order.Assignee.ShouldBe(assignee);
        order.Status.ShouldBe(WorkOrderStatus.Assigned);
        order.DueDate.ShouldBe(due);
        order.CompletedDate.ShouldBe(completed);
        order.Building.ShouldBe("Sanctuary");
        order.RoomNumber.ShouldBe("101");
        order.Priority.ShouldBe(WorkOrderPriority.High);
    }

    [Test]
    public void ShouldMarkWaitingOnVendor_WithoutChangingStatus()
    {
        var since = new DateTime(2026, 10, 14, 9, 0, 0, DateTimeKind.Utc);

        var order = WorkOrderBuilder.Create()
            .WithStatus(WorkOrderStatus.InProgress)
            .WaitingOnVendorSince(since)
            .Build();

        order.Status.ShouldBe(WorkOrderStatus.InProgress);
        order.WaitingOnVendorSince.ShouldBe(since);
    }
}
