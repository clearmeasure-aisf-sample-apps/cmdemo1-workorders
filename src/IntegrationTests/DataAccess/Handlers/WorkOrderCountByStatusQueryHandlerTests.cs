using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
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
        var userName = TestData.UserName("wocbs");
        var creator = new Employee(userName, "First", "Last", TestData.Email(userName));
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(new WorkOrder { Creator = creator, Number = TestData.Number("C"), Status = WorkOrderStatus.Draft });
            context.Add(new WorkOrder { Creator = creator, Number = TestData.Number("C"), Status = WorkOrderStatus.Assigned });
            context.Add(new WorkOrder { Creator = creator, Number = TestData.Number("C"), Status = WorkOrderStatus.Assigned });
            context.Add(new WorkOrder { Creator = creator, Number = TestData.Number("C"), Status = WorkOrderStatus.InProgress });
            context.Add(new WorkOrder { Creator = creator, Number = TestData.Number("C"), Status = WorkOrderStatus.Complete });
            context.Add(new WorkOrder { Creator = creator, Number = TestData.Number("C"), Status = WorkOrderStatus.Cancelled });
            context.Add(new WorkOrder { Creator = creator, Number = TestData.Number("C"), Status = WorkOrderStatus.Cancelled });
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var counts = await new WorkOrderCountByStatusQueryHandler(dataContext).Handle(new WorkOrderCountByStatusQuery());

        foreach (var status in WorkOrderStatus.GetAllItems())
        {
            var specification = new WorkOrderSpecificationQuery();
            specification.MatchStatus(status);
            var found = await new WorkOrderSearchHandler(dataContext, TimeProvider.System).Handle(specification);
            counts[status.Key].ShouldBe(found.Length, status.Key);
        }

        (counts.Values.Sum() - before.Values.Sum()).ShouldBe(7);
    }

    private static async Task<IReadOnlyDictionary<string, int>> CountsAsync()
    {
        var handler = new WorkOrderCountByStatusQueryHandler(TestHost.GetRequiredService<DataContext>());
        return await handler.Handle(new WorkOrderCountByStatusQuery());
    }
}
