using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class ReformatWorkOrderCommandHandlerTests : IntegratedTestBase
{
    [Test]
    public async Task Handle_WhenWorkOrderExists_AppliesReformatAndSaves()
    {
        var workOrder = await SeedDraftAsync("old title", "old description");
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await bus.Send(new ReformatWorkOrderCommand(workOrder.Number!, "New Title", "New description."));

        result.ShouldNotBeNull();
        result.Title.ShouldBe("New Title");
        result.Description.ShouldBe("New description.");

        await using var context = TestHost.GetRequiredService<DbContext>();
        var reloaded = await context.Set<WorkOrder>().SingleAsync(order => order.Number == workOrder.Number);
        reloaded.Title.ShouldBe("New Title");
        reloaded.Description.ShouldBe("New description.");
    }

    [Test]
    public async Task Handle_WhenWorkOrderMissing_ReturnsNull()
    {
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await bus.Send(new ReformatWorkOrderCommand("WO-MISSING-REFORMAT", "Title", "Description"));

        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WhenDescriptionTooLong_TruncatesOnSave()
    {
        var workOrder = await SeedDraftAsync("title", "short");
        var longDescription = new string('z', WorkOrder.DescriptionMaxLength + 25);
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await bus.Send(new ReformatWorkOrderCommand(workOrder.Number!, "Title", longDescription));

        result!.Description!.Length.ShouldBe(WorkOrder.DescriptionMaxLength);
    }

    private static async Task<WorkOrder> SeedDraftAsync(string title, string description)
    {
        var creator = new Employee(TestData.UserName("reformat"), "Re", "Format", "reformat@example.com");
        var workOrder = new WorkOrder
        {
            Number = TestData.Number("R"),
            Title = title,
            Description = description,
            Status = WorkOrderStatus.Draft,
            Creator = creator
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();
        return workOrder;
    }
}
