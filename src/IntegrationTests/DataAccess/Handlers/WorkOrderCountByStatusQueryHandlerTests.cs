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
        new DatabaseTests().Clean();

        var creator = new Employee("wocbs_user", "First", "Last", "email@test.com");

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            var order1 = new WorkOrder { Creator = creator, Number = "WOCBS-1", Status = WorkOrderStatus.Draft };
            var order2 = new WorkOrder { Creator = creator, Number = "WOCBS-2", Status = WorkOrderStatus.Draft };
            var order3 = new WorkOrder { Creator = creator, Number = "WOCBS-3", Status = WorkOrderStatus.InProgress };
            context.Add(order1);
            context.Add(order2);
            context.Add(order3);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrderCountByStatusQueryHandler(dataContext);
        var result = await handler.Handle(new WorkOrderCountByStatusQuery());

        result.Count.ShouldBe(WorkOrderStatus.GetAllItems().Length);
        result[WorkOrderStatus.Draft.Key].ShouldBe(2);
        result[WorkOrderStatus.Assigned.Key].ShouldBe(0);
        result[WorkOrderStatus.InProgress.Key].ShouldBe(1);
        result[WorkOrderStatus.Complete.Key].ShouldBe(0);
        result[WorkOrderStatus.Cancelled.Key].ShouldBe(0);
    }

    [Test]
    public async Task ShouldMatchSearchTotal_ForEachStatus()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("wocbs_search", "First", "Last", "search@test.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(new WorkOrder { Creator = creator, Number = "WOCBS-A", Status = WorkOrderStatus.Draft });
            context.Add(new WorkOrder { Creator = creator, Number = "WOCBS-B", Status = WorkOrderStatus.Assigned });
            context.Add(new WorkOrder { Creator = creator, Number = "WOCBS-C", Status = WorkOrderStatus.Assigned });
            context.Add(new WorkOrder { Creator = creator, Number = "WOCBS-D", Status = WorkOrderStatus.InProgress });
            context.Add(new WorkOrder { Creator = creator, Number = "WOCBS-E", Status = WorkOrderStatus.Complete });
            context.Add(new WorkOrder { Creator = creator, Number = "WOCBS-F", Status = WorkOrderStatus.Cancelled });
            context.Add(new WorkOrder { Creator = creator, Number = "WOCBS-G", Status = WorkOrderStatus.Cancelled });
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

        counts.Values.Sum().ShouldBe(7);
    }
}
