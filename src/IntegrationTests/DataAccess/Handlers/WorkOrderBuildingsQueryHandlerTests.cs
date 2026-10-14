using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderBuildingsQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnDistinctNonBlankBuildingsSorted()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("bldg", "Building", "Creator", "b@example.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.AddRange(
                new WorkOrder { Creator = creator, Number = "B000001", Building = "Sanctuary" },
                new WorkOrder { Creator = creator, Number = "B000002", Building = " Sanctuary " },
                new WorkOrder { Creator = creator, Number = "B000003", Building = "Kitchen" },
                new WorkOrder { Creator = creator, Number = "B000004", Building = "   " },
                new WorkOrder { Creator = creator, Number = "B000005", Building = null },
                new WorkOrder { Creator = creator, Number = "B000006", Building = "fellowship Hall" });
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderBuildingsQueryHandler(TestHost.GetRequiredService<DataContext>());
        var buildings = await handler.Handle(new WorkOrderBuildingsQuery());

        buildings.ShouldBe(["fellowship Hall", "Kitchen", "Sanctuary"]);
    }
}
