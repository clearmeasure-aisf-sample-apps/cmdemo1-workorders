using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderSnapshotTests
{
    private static readonly DateTime UtcNow = new(2026, 10, 13, 18, 0, 0, DateTimeKind.Utc);

    private static readonly Employee Creator = EmployeeBuilder.Create("snapcreator").WithId(Guid.NewGuid()).Build();

    [Test]
    public void ShouldCountAndAverageDaysToClose_WhenCompletedWithinWindow()
    {
        var snapshot = Snapshot(
            Completed(6, 2).Build(),
            Completed(10, 0).Build(),
            Completed(90, 40).Build());

        snapshot.CompletionsWithin(30).ShouldBe((2, 7.0));
    }

    [Test]
    public void ShouldReportNoAverage_WhenNothingCompletedWithinWindow()
    {
        var snapshot = Snapshot(Completed(90, 40).Build());

        snapshot.CompletionsWithin(30).ShouldBe((0, (double?)null));
    }

    [Test]
    public void ShouldReturnOnlyListedNumbers_IgnoringCase_WhenWithNumbers()
    {
        var snapshot = Snapshot(Numbered("AB12").Build(), Numbered("CD34").Build(), Numbered("EF56").Build());

        var found = snapshot.WithNumbers(new HashSet<string>(["ab12", "zz99"], StringComparer.OrdinalIgnoreCase));

        found.Select(wo => wo.Number).ShouldBe(["AB12"]);
    }

    [Test]
    public void ShouldSplitOpenAssignedAndCreated_WhenSnapshotRead()
    {
        var assignee = EmployeeBuilder.Create("snapassignee").WithId(Guid.NewGuid()).Build();
        var assigned = WorkOrderBuilder.For(Creator, WorkOrderStatus.Assigned).AssignedTo(assignee).Build();
        var done = WorkOrderBuilder.For(Creator, WorkOrderStatus.Complete).Build();
        var snapshot = Snapshot(assigned, done);

        snapshot.Open.ShouldBe([assigned]);
        snapshot.AssignedTo(assignee.Id).ShouldBe([assigned]);
        snapshot.CreatedBy(Creator.Id).Count().ShouldBe(2);
        snapshot.Today.ShouldBe(new DateOnly(2026, 10, 13));
    }

    internal static WorkOrderSnapshot Snapshot(params WorkOrder[] workOrders) =>
        new(workOrders, FixedUtcClock.At(new DateTimeOffset(UtcNow)));

    private static WorkOrderBuilder Completed(int createdDaysAgo, int completedDaysAgo) =>
        WorkOrderBuilder.For(Creator, WorkOrderStatus.Complete)
            .CreatedAt(UtcNow.AddDays(-createdDaysAgo))
            .CompletedAt(UtcNow.AddDays(-completedDaysAgo));

    private static WorkOrderBuilder Numbered(string number) =>
        WorkOrderBuilder.Create().WithNumber(number).CreatedBy(Creator);
}
