using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderSnapshotSearchTests
{
    private static readonly DateTime UtcNow = new(2026, 10, 13, 18, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 10, 13);

    private static readonly Employee Creator =
        EmployeeBuilder.Create("searchcreator").WithFirstName("Martha").WithId(Guid.NewGuid()).Build();

    private static readonly Employee Assignee =
        EmployeeBuilder.Create("searchassignee").WithFirstName("Gus").WithId(Guid.NewGuid()).Build();

    [Test]
    public void ShouldReturnEveryWorkOrder_NewestCreatedFirst_WhenNoFilter()
    {
        var older = Order("B").CreatedAt(UtcNow.AddDays(-2)).Build();
        var newer = Order("A").CreatedAt(UtcNow.AddDays(-1)).Build();

        WorkOrderSnapshotSearch.Run(WorkOrderSnapshotTests.Snapshot(older, newer), new WorkOrderSpecificationQuery())
            .ShouldBe([newer, older]);
    }

    [Test]
    public void ShouldMatchAssigneeCreatorStatusAndPriority_WhenSet()
    {
        var match = Order("A").AssignedTo(Assignee).WithStatus(WorkOrderStatus.Assigned)
            .WithPriority(WorkOrderPriority.High).Build();
        var otherPriority = Order("B").AssignedTo(Assignee).WithStatus(WorkOrderStatus.Assigned).Build();
        var unassigned = Order("C").WithStatus(WorkOrderStatus.Assigned).WithPriority(WorkOrderPriority.High).Build();
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchAssignee(Assignee);
        specification.MatchCreator(Creator);
        specification.MatchStatus(WorkOrderStatus.Assigned);
        specification.MatchPriority(WorkOrderPriority.High);

        WorkOrderSnapshotSearch.Run(WorkOrderSnapshotTests.Snapshot(match, otherPriority, unassigned), specification)
            .ShouldBe([match]);
    }

    [Test]
    public void ShouldKeepOverdueOpenWorkOrders_WhenOverdueOnly()
    {
        var overdue = Order("A").WithStatus(WorkOrderStatus.Assigned).DueOn(Today.AddDays(-1)).Build();
        var dueToday = Order("B").WithStatus(WorkOrderStatus.Assigned).DueOn(Today).Build();
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchOverdueOnly(true);

        WorkOrderSnapshotSearch.Run(WorkOrderSnapshotTests.Snapshot(overdue, dueToday), specification)
            .ShouldBe([overdue]);
    }

    [Test]
    public void ShouldKeepOpenWorkOrdersDueThisWeek_WhenDueThisWeekOnly()
    {
        var thisWeek = Order("A").WithStatus(WorkOrderStatus.Draft).DueOn(Today.AddDays(6)).Build();
        var nextWeek = Order("B").WithStatus(WorkOrderStatus.Draft).DueOn(Today.AddDays(7)).Build();
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchDueThisWeekOnly(true);

        WorkOrderSnapshotSearch.Run(WorkOrderSnapshotTests.Snapshot(thisWeek, nextWeek), specification)
            .ShouldBe([thisWeek]);
    }

    [Test]
    public void ShouldKeepOpenAndWaitingOnVendor_WhenThoseFiltersSet()
    {
        var waiting = Order("A").WithStatus(WorkOrderStatus.InProgress).WaitingOnVendorSince(UtcNow).Build();
        var inProgress = Order("B").WithStatus(WorkOrderStatus.InProgress).Build();
        var complete = Order("C").WithStatus(WorkOrderStatus.Complete).WaitingOnVendorSince(UtcNow).Build();
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchOpenOnly(true);
        specification.MatchWaitingOnVendorOnly(true);

        WorkOrderSnapshotSearch.Run(WorkOrderSnapshotTests.Snapshot(waiting, inProgress, complete), specification)
            .ShouldBe([waiting]);
    }

    [Test]
    public void ShouldMatchTrimmedBuildingAndRoomIgnoringCase_WhenNamed()
    {
        var match = Order("A").InBuilding(" Fellowship Hall ").InRoom("101").Build();
        var otherRoom = Order("B").InBuilding("Fellowship Hall").InRoom("102").Build();
        var noBuilding = Order("C").InRoom("101").Build();
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchBuilding("fellowship hall");
        specification.MatchRoom(" 101 ");

        WorkOrderSnapshotSearch.Run(WorkOrderSnapshotTests.Snapshot(match, otherRoom, noBuilding), specification)
            .ShouldBe([match]);
    }

    [Test]
    public void ShouldKeepBlankBuildingAndRoom_WhenNoBuildingAndNoRoom()
    {
        var blank = Order("A").InBuilding(" ").Build();
        var named = Order("B").InBuilding("Sanctuary").Build();
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchNoBuilding(true);
        specification.MatchNoRoom(true);

        WorkOrderSnapshotSearch.Run(WorkOrderSnapshotTests.Snapshot(blank, named), specification)
            .ShouldBe([blank]);
    }

    [Test]
    public void ShouldKeepRecentCompletionsMatchingText_WhenCompletedWithinDaysAndSearchText()
    {
        var match = Order("A").WithTitle("Fix the Organ pedal").WithStatus(WorkOrderStatus.Complete)
            .CompletedAt(UtcNow.AddDays(-3)).Build();
        var oldCompletion = Order("B").WithTitle("Organ tuning").WithStatus(WorkOrderStatus.Complete)
            .CompletedAt(UtcNow.AddDays(-60)).Build();
        var otherTitle = Order("C").WithTitle("Paint the hall").WithStatus(WorkOrderStatus.Complete)
            .CompletedAt(UtcNow.AddDays(-3)).Build();
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCompletedWithinDays(30);
        specification.MatchSearchText("  ORGAN ");

        WorkOrderSnapshotSearch.Run(WorkOrderSnapshotTests.Snapshot(match, oldCompletion, otherTitle), specification)
            .ShouldBe([match]);
    }

    [Test]
    public void ShouldOrderByDueDateWithBlankDueDatesLast_WhenOrderedByDueDate()
    {
        var later = Order("A").DueOn(Today.AddDays(5)).Build();
        var none = Order("B").Build();
        var sooner = Order("C").DueOn(Today.AddDays(1)).Build();
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchDueDateOrder(orderByDueDate: true);

        WorkOrderSnapshotSearch.Run(WorkOrderSnapshotTests.Snapshot(later, none, sooner), specification)
            .ShouldBe([sooner, later, none]);
    }

    [Test]
    public void ShouldOrderByDueDateNewestFirstWithBlankDueDatesLast_WhenDescending()
    {
        var later = Order("A").DueOn(Today.AddDays(5)).Build();
        var none = Order("B").Build();
        var sooner = Order("C").DueOn(Today.AddDays(1)).Build();
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchDueDateOrder(orderByDueDate: true, descending: true);

        WorkOrderSnapshotSearch.Run(WorkOrderSnapshotTests.Snapshot(sooner, none, later), specification)
            .ShouldBe([later, sooner, none]);
    }


    private static WorkOrderBuilder Order(string number) =>
        WorkOrderBuilder.Create().WithNumber(number).CreatedBy(Creator).CreatedAt(UtcNow.AddDays(-1));
}
