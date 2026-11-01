using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WorkOrderQueryFiltersTests
{
    private static readonly TimeProvider Clock = new FixedClock(new DateTimeOffset(2026, 10, 13, 18, 0, 0, TimeSpan.Zero));

    [Test]
    public void ShouldApplyAllFilters_WhenSpecificationPopulated()
    {
        var assignee = new Employee("a", "a", "a", "a");
        var creator = new Employee("c", "c", "c", "c");
        var orders = new[]
        {
            new WorkOrder { Number = "1", Assignee = assignee, Creator = creator, Status = WorkOrderStatus.Assigned },
            new WorkOrder { Number = "2", Assignee = assignee, Creator = creator, Status = WorkOrderStatus.Draft }
        }.AsQueryable();

        var filtered = WorkOrderQueryFilters.Apply(orders, assignee, creator, WorkOrderStatus.Assigned).ToArray();

        filtered.Length.ShouldBe(1);
        filtered[0].Number.ShouldBe("1");
    }

    [Test]
    public void ShouldFilterByPriority()
    {
        var orders = new[]
        {
            new WorkOrder { Number = "1", Priority = WorkOrderPriority.High },
            new WorkOrder { Number = "2", Priority = WorkOrderPriority.Low },
            new WorkOrder { Number = "3" }
        }.AsQueryable();
        var query = new WorkOrderSpecificationQuery();
        query.MatchPriority(WorkOrderPriority.High);

        Numbers(WorkOrderQueryFilters.Apply(orders, query, Clock)).ShouldBe(["1"]);
    }

    [Test]
    public void ShouldFilterByBuilding()
    {
        var orders = new[]
        {
            new WorkOrder { Number = "1", Building = "Sanctuary" },
            new WorkOrder { Number = "2", Building = " Sanctuary " },
            new WorkOrder { Number = "3", Building = "Kitchen" },
            new WorkOrder { Number = "4" }
        }.AsQueryable();
        var query = new WorkOrderSpecificationQuery();
        query.MatchBuilding("Sanctuary");

        Numbers(WorkOrderQueryFilters.Apply(orders, query, Clock)).ShouldBe(["1", "2"]);
    }

    [Test]
    public void ShouldFilterOpenOnly()
    {
        var orders = new[]
        {
            new WorkOrder { Number = "1", Status = WorkOrderStatus.Draft },
            new WorkOrder { Number = "2", Status = WorkOrderStatus.Assigned },
            new WorkOrder { Number = "3", Status = WorkOrderStatus.InProgress },
            new WorkOrder { Number = "4", Status = WorkOrderStatus.Complete },
            new WorkOrder { Number = "5", Status = WorkOrderStatus.Cancelled }
        }.AsQueryable();
        var query = new WorkOrderSpecificationQuery();
        query.MatchOpenOnly(true);

        Numbers(WorkOrderQueryFilters.Apply(orders, query, Clock)).ShouldBe(["1", "2", "3"]);
    }

    [Test]
    public void ShouldFilterCompletedWithinDays()
    {
        var now = Clock.GetUtcNow().DateTime;
        var orders = new[]
        {
            new WorkOrder { Number = "1", Status = WorkOrderStatus.Complete, CompletedDate = now.AddDays(-2) },
            new WorkOrder { Number = "2", Status = WorkOrderStatus.Complete, CompletedDate = now.AddDays(-31) },
            new WorkOrder { Number = "3", Status = WorkOrderStatus.InProgress, CompletedDate = now.AddDays(-1) },
            new WorkOrder { Number = "4", Status = WorkOrderStatus.Complete }
        }.AsQueryable();
        var query = new WorkOrderSpecificationQuery();
        query.MatchCompletedWithinDays(30);

        Numbers(WorkOrderQueryFilters.Apply(orders, query, Clock)).ShouldBe(["1"]);
    }

    [Test]
    public void ShouldFilterBySearchText_AcrossNumberTitleRoomAndNames_IgnoringCase()
    {
        var ned = new Employee("ned", "Ned", "Flanders", "n@x");
        var willie = new Employee("willie", "Groundskeeper", "Willie", "w@x");
        var orders = new[]
        {
            new WorkOrder { Number = "38173BA", Title = "Mow", RoomNumber = "Grounds", Creator = ned },
            new WorkOrder { Number = "1", Title = "Fix the ORGAN", RoomNumber = "Loft", Creator = ned },
            new WorkOrder { Number = "2", Title = "Paint", RoomNumber = "Organ room", Creator = ned },
            new WorkOrder { Number = "3", Title = "Lights", RoomNumber = null, Creator = ned, Assignee = willie },
            new WorkOrder { Number = "4", Title = null, RoomNumber = null, Creator = null }
        }.AsQueryable();

        Numbers(WorkOrderQueryFilters.Apply(orders, Search("173b"), Clock)).ShouldBe(["38173BA"]);
        Numbers(WorkOrderQueryFilters.Apply(orders, Search(" organ "), Clock)).ShouldBe(["1", "2"]);
        Numbers(WorkOrderQueryFilters.Apply(orders, Search("WILLIE"), Clock)).ShouldBe(["3"]);
        Numbers(WorkOrderQueryFilters.Apply(orders, Search("flanders"), Clock)).ShouldBe(["38173BA", "1", "2", "3"]);
        Numbers(WorkOrderQueryFilters.Apply(orders, Search("   "), Clock)).Length.ShouldBe(5);
    }

    [Test]
    public void ShouldTreatOrderDueBeforeChurchTodayAsOverdue()
    {
        var lateEveningUtc = new FixedClock(new DateTimeOffset(2026, 10, 14, 3, 0, 0, TimeSpan.Zero));
        var orders = new[]
        {
            new WorkOrder { Number = "1", Status = WorkOrderStatus.InProgress, DueDate = new DateOnly(2026, 10, 12) },
            new WorkOrder { Number = "2", Status = WorkOrderStatus.InProgress, DueDate = new DateOnly(2026, 10, 13) },
            new WorkOrder { Number = "3", Status = WorkOrderStatus.Complete, DueDate = new DateOnly(2026, 10, 1) }
        }.AsQueryable();
        var query = new WorkOrderSpecificationQuery();
        query.MatchOverdueOnly(true);

        Numbers(WorkOrderQueryFilters.Apply(orders, query, lateEveningUtc)).ShouldBe(["1"]);
    }

    [Test]
    public void ShouldAgreeWithFilters_WhenPredicatesUsedAsExpressions()
    {
        var lateEveningUtc = new FixedClock(new DateTimeOffset(2026, 10, 14, 3, 0, 0, TimeSpan.Zero));
        var today = new DateOnly(2026, 10, 13);
        var now = lateEveningUtc.GetUtcNow().DateTime;
        var orders = new[]
        {
            new WorkOrder { Number = "1", Status = WorkOrderStatus.Draft, DueDate = today.AddDays(-1) },
            new WorkOrder { Number = "2", Status = WorkOrderStatus.Assigned, DueDate = today },
            new WorkOrder { Number = "3", Status = WorkOrderStatus.InProgress },
            new WorkOrder { Number = "4", Status = WorkOrderStatus.Complete, CompletedDate = now.AddDays(-30), DueDate = today.AddDays(-9) },
            new WorkOrder { Number = "5", Status = WorkOrderStatus.Complete, CompletedDate = now.AddDays(-30).AddSeconds(-1) },
            new WorkOrder { Number = "6", Status = WorkOrderStatus.Cancelled, DueDate = today.AddDays(-9) }
        }.AsQueryable();

        Numbers(orders.Where(WorkOrderQueryFilters.IsOpen())).ShouldBe(Numbers(WorkOrderQueryFilters.OpenOnly(orders)));
        Numbers(orders.Where(WorkOrderQueryFilters.IsOpen())).ShouldBe(["1", "2", "3"]);
        Numbers(orders.Where(WorkOrderQueryFilters.IsOverdue(today))).ShouldBe(Numbers(WorkOrderQueryFilters.Overdue(orders, today)));
        Numbers(orders.Where(WorkOrderQueryFilters.IsOverdue(today))).ShouldBe(["1"]);
        Numbers(orders.Where(WorkOrderQueryFilters.IsCompletedWithin(30, lateEveningUtc)))
            .ShouldBe(Numbers(WorkOrderQueryFilters.CompletedWithin(orders, 30, lateEveningUtc)));
        Numbers(orders.Where(WorkOrderQueryFilters.IsCompletedWithin(30, lateEveningUtc))).ShouldBe(["4"]);
    }

    [Test]
    public void ShouldProjectKeysAndFlags_WhenCountRowApplied()
    {
        var today = new DateOnly(2026, 10, 13);
        var now = Clock.GetUtcNow().DateTime;
        var orders = new[]
        {
            new WorkOrder { Number = "1", Building = " Hall ", RoomNumber = "101 ", Status = WorkOrderStatus.Assigned, DueDate = today.AddDays(-1) },
            new WorkOrder { Number = "2", Building = null, RoomNumber = "  ", Status = WorkOrderStatus.Complete, CompletedDate = now.AddDays(-1) },
            new WorkOrder { Number = "3", Building = "", RoomNumber = null, Status = WorkOrderStatus.Cancelled, DueDate = today.AddDays(-1) }
        }.AsQueryable();

        var rows = orders.Select(WorkOrderQueryFilters.CountRow(today, Clock, 30)).ToArray();

        rows.Select(r => (r.Number, r.Building, r.Room, r.Open, r.Overdue, r.Completed)).ShouldBe(
        [
            ("1", "Hall", "101", 1, 1, 0),
            ("2", "", "", 0, 0, 1),
            ("3", "", "", 0, 0, 0)
        ]);
    }

    private static WorkOrderSpecificationQuery Search(string text)
    {
        var query = new WorkOrderSpecificationQuery();
        query.MatchSearchText(text);
        return query;
    }

    private static string?[] Numbers(IQueryable<WorkOrder> query) => query.Select(o => o.Number).ToArray();

    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
