using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderCountByStatusQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnLiveCountsFromDatabase()
    {
        var before = await CountsAsync();
        var userName = TestData.UserName("wocbs");
        var creator = new Employee(userName, "First", "Last", TestData.Email(userName));

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            var order1 = new WorkOrder { Creator = creator, Number = TestData.Number("C"), Status = WorkOrderStatus.Draft };
            var order2 = new WorkOrder { Creator = creator, Number = TestData.Number("C"), Status = WorkOrderStatus.Draft };
            var order3 = new WorkOrder { Creator = creator, Number = TestData.Number("C"), Status = WorkOrderStatus.InProgress };
            context.Add(order1);
            context.Add(order2);
            context.Add(order3);
            await context.SaveChangesAsync();
        }

        var result = await CountsAsync();

        result.Count.ShouldBe(WorkOrderStatus.GetAllItems().Length);
        (result[WorkOrderStatus.Draft.Key] - before[WorkOrderStatus.Draft.Key]).ShouldBe(2);
        (result[WorkOrderStatus.Assigned.Key] - before[WorkOrderStatus.Assigned.Key]).ShouldBe(0);
        (result[WorkOrderStatus.InProgress.Key] - before[WorkOrderStatus.InProgress.Key]).ShouldBe(1);
        (result[WorkOrderStatus.Complete.Key] - before[WorkOrderStatus.Complete.Key]).ShouldBe(0);
        (result[WorkOrderStatus.Cancelled.Key] - before[WorkOrderStatus.Cancelled.Key]).ShouldBe(0);
    }

    [Test]
    public async Task ShouldMatchSearchTotal_ForEachStatus()
    {
        var before = await CountsAsync();
        await GivenOneOfEachStatusAsync();

        var counts = await CountsAsync();
        await ThenMatchesSearchTotalsAsync(counts);

        (counts.Values.Sum() - before.Values.Sum()).ShouldBe(7);
    }

    [Test]
    public async Task ShouldMatchPerStatusFilterCounts()
    {
        await GivenPerStatusFilterOrdersAsync();

        var counts = await CountsAsync();
        await ThenMatchesFilterCountsAsync(counts);

        counts.Count.ShouldBe(WorkOrderStatus.GetAllItems().Length);
    }

    private static async Task GivenOneOfEachStatusAsync()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("wocbs"))
            .WithFirstName("First").WithLastName("Last").Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Cancelled).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Cancelled).Build());
    }

    private static async Task GivenPerStatusFilterOrdersAsync()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("wocbs"))
            .WithFirstName("First").WithLastName("Last").Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Cancelled).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Cancelled).Build());
    }

    private static async Task ThenMatchesSearchTotalsAsync(IReadOnlyDictionary<string, int> counts)
    {
        var dataContext = TestHost.GetRequiredService<DataContext>();
        foreach (var status in WorkOrderStatus.GetAllItems())
        {
            var specification = new WorkOrderSpecificationQuery();
            specification.MatchStatus(status);
            var found = await new WorkOrderSearchHandler(dataContext, TimeProvider.System, new ChurchCalendar(TimeProvider.System)).Handle(specification);
            counts[status.Key].ShouldBe(found.Length, status.Key);
        }
    }

    private static async Task ThenMatchesFilterCountsAsync(IReadOnlyDictionary<string, int> counts)
    {
        var dataContext = TestHost.GetRequiredService<DataContext>();
        var orders = dataContext.Set<WorkOrder>().AsNoTracking();
        foreach (var status in WorkOrderStatus.GetAllItems())
        {
            var specification = new WorkOrderSearchSpecification();
            specification.MatchStatus(status);
            // Characterization against the shared filter helper; not a product query path.
#pragma warning disable CM0002
            var expected = await WorkOrderQueryFilters.Apply(orders, specification).CountAsync();
#pragma warning restore CM0002
            counts[status.Key].ShouldBe(expected, status.Key);
        }
    }

    private static async Task<IReadOnlyDictionary<string, int>> CountsAsync()
    {
        var handler = new WorkOrderCountByStatusQueryHandler(TestHost.GetRequiredService<DataContext>());
        return await handler.Handle(new WorkOrderCountByStatusQuery());
    }
}
