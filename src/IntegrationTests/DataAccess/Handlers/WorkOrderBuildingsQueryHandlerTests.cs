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
        var tag = TestData.Tag();
        var userName = TestData.UserName("bldg");
        var creator = new Employee(userName, "Building", "Creator", TestData.Email(userName));
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.AddRange(
                new WorkOrder { Creator = creator, Number = TestData.Number("B"), Building = $"Sanctuary {tag}" },
                new WorkOrder { Creator = creator, Number = TestData.Number("B"), Building = $" Sanctuary {tag} " },
                new WorkOrder { Creator = creator, Number = TestData.Number("B"), Building = $"Kitchen {tag}" },
                new WorkOrder { Creator = creator, Number = TestData.Number("B"), Building = "   " },
                new WorkOrder { Creator = creator, Number = TestData.Number("B"), Building = null },
                new WorkOrder { Creator = creator, Number = TestData.Number("B"), Building = $"fellowship Hall {tag}" });
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderBuildingsQueryHandler(TestHost.GetRequiredService<DataContext>());
        var buildings = await handler.Handle(new WorkOrderBuildingsQuery());

        buildings.Where(b => b.Contains(tag)).ShouldBe([$"fellowship Hall {tag}", $"Kitchen {tag}", $"Sanctuary {tag}"]);
        buildings.ShouldAllBe(b => !string.IsNullOrWhiteSpace(b));
    }
}
