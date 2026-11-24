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

    [Test]
    public void ManyDrafts_ReturnsNumberedDraftCatalog()
    {
        var orders = WorkOrderBuilder.ManyDrafts(3);

        orders.Length.ShouldBe(3);
        orders[0].Number.ShouldBe("WO-001");
        orders[0].Title.ShouldBe("Order 1");
        orders[0].Status.ShouldBe(WorkOrderStatus.Draft);
        orders[2].Number.ShouldBe("WO-003");
    }

    [Test]
    public void OnePerStatus_CyclesStatusesAndEvenRowExtras()
    {
        var orders = WorkOrderBuilder.OnePerStatus(1);

        orders.Length.ShouldBe(5);
        orders[0].Status.ShouldBe(WorkOrderStatus.Draft);
        orders[0].Assignee.ShouldBeNull();
        orders[1].Status.ShouldBe(WorkOrderStatus.Assigned);
        orders[1].Priority.ShouldBe(WorkOrderPriority.High);
        orders[1].Assignee!.UserName.ShouldBe("gwillie");
        orders[1].RoomNumber.ShouldBe("101");
        orders[1].DueDate.ShouldBe(new DateOnly(2000, 1, 1));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void ShouldBuildCompletionsJustOutsideWindow_WhenCountedBackFromTheWindowStart(int daysBack)
    {
        var creator = EmployeeBuilder.Create("sam").WithFirstName("Sam").WithLastName("Stern").Build();
        var windowStart = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var orders = WorkOrderBuilder.CompletedJustOutsideWindow(creator, windowStart, 3);

        var expectedCompleted = windowStart.AddDays(-daysBack);
        var order = orders[daysBack - 1];

        order.CompletedDate.ShouldBe(expectedCompleted);
        order.Status.ShouldBe(WorkOrderStatus.Complete);
        order.DueDate.ShouldBe(DateOnly.FromDateTime(expectedCompleted));
        order.Creator.ShouldBe(creator);
        orders.Length.ShouldBe(3);
        orders.All(o => o.DueDate.HasValue).ShouldBeTrue();
        orders.Select(o => o.Number).ShouldBeUnique();
    }

    [Test]
    public void ShouldBuildCompletionsJustOutsideWindow_WhenCountedBackFromTheWindowStart()
    {
        var creator = EmployeeBuilder.Create("creator").Build();
        var windowStart = new DateTime(2026, 2, 1, 6, 0, 0, DateTimeKind.Unspecified);

        var orders = WorkOrderBuilder.CompletedJustOutsideWindow(creator, windowStart, 3);

        orders.Length.ShouldBe(3);
        orders[0].CompletedDate.ShouldBe(windowStart.AddDays(-1));
        orders[1].CompletedDate.ShouldBe(windowStart.AddDays(-2));
        orders[2].CompletedDate.ShouldBe(windowStart.AddDays(-3));
        orders.ShouldAllBe(o => o.Status == WorkOrderStatus.Complete);
        orders.ShouldAllBe(o => o.DueDate != null);
        orders.ShouldAllBe(o => o.Creator == creator);
        orders.Select(o => o.Number).Distinct().Count().ShouldBe(3);
    }
}
