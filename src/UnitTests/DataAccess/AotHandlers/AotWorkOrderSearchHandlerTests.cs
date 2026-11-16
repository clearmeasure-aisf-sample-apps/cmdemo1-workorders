using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary>
/// <see cref="AotWorkOrderSearchHandler"/> applies each search filter and order in memory with the same result as the
/// JIT handler's SQL.
/// </summary>
[TestFixture]
public class AotWorkOrderSearchHandlerTests
{
    private AotParityDatabase _db = null!;

    private static IEnumerable<TestCaseData> Searches()
    {
        yield return new TestCaseData(new WorkOrderSpecificationQuery()).SetArgDisplayNames("no filter");
        yield return new TestCaseData(new WorkOrderSpecificationQuery { StatusKey = WorkOrderStatus.Assigned.Key })
            .SetArgDisplayNames("status");
        yield return new TestCaseData(new WorkOrderSpecificationQuery { OverdueOnly = true }).SetArgDisplayNames("overdue");
        yield return new TestCaseData(new WorkOrderSpecificationQuery { Building = "Main", Room = "101" })
            .SetArgDisplayNames("building and room");
        yield return new TestCaseData(new WorkOrderSpecificationQuery { NoBuilding = true }).SetArgDisplayNames("no building");
        yield return new TestCaseData(new WorkOrderSpecificationQuery { OpenOnly = true, Priority = WorkOrderPriority.High })
            .SetArgDisplayNames("open high priority");
        yield return new TestCaseData(new WorkOrderSpecificationQuery { CompletedWithinDays = 30 })
            .SetArgDisplayNames("completed in 30 days");
        yield return new TestCaseData(new WorkOrderSpecificationQuery { SearchText = "aaa0003" }).SetArgDisplayNames("text");
        yield return new TestCaseData(new WorkOrderSpecificationQuery { OrderByDueDate = true, DueDateDescending = true })
            .SetArgDisplayNames("due date descending");
    }

    [SetUp]
    public async Task SetUp() => _db = await AotParityDatabase.SeedAsync();

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [TestCaseSource(nameof(Searches))]
    public async Task Handle_Should_FindTheSameWorkOrdersInTheSameOrder(WorkOrderSpecificationQuery search)
    {
        var jit = await new WorkOrderSearchHandler(_db.Context, _db.Clock).Handle(search);
        var aot = await new AotWorkOrderSearchHandler(_db.Context, _db.Clock).Handle(search);

        aot.Select(order => order.Number).ShouldBe(jit.Select(order => order.Number));
    }
}
