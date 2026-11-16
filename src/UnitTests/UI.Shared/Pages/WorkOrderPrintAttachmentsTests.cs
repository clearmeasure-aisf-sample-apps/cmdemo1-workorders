using Bunit;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderPrintAttachmentsTests
{
    private static string Id(WorkOrderPrint.Elements e) => $"[data-testid='{e}']";

    [Test]
    public async Task ShouldNameAttachmentListByItsHeading()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var wo = new WorkOrder
        {
            Number = "WO-1", Title = "Leaky roof", Status = WorkOrderStatus.Assigned,
            Priority = WorkOrderPriority.High, Building = "Sanctuary", RoomNumber = "12",
            Description = "Drips", Instructions = "Bucket",
            Creator = new Employee("a", "Ann", "Lee", "a@x.com"),
            Assignee = new Employee("b", "Bob", "Ray", "b@x.com"),
            CreatedDate = new DateTime(2025, 1, 15, 18, 30, 0, DateTimeKind.Utc),
        };
        ctx.Services.AddSingleton<IBus>(new StubBus(wo, [new WorkOrderAttachment { FileName = "roof.jpg" }]));
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var c = ctx.Render<WorkOrderPrint>(p => p.Add(x => x.Number, "WO-1"));

        c.Find(Id(WorkOrderPrint.Elements.Attachments)).GetAttribute("aria-labelledby")
            .ShouldBe("print-attachments-heading");
        var heading = c.Find("#print-attachments-heading");
        heading.TagName.ShouldBe("H2");
        heading.TextContent.ShouldBe("Attachments");
    }

    private class StubBus(WorkOrder? result, WorkOrderAttachment[]? attachments = null) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderByNumberQuery)
                return Task.FromResult((TResponse)(object)result!);
            if (request is WorkOrderAttachmentsQuery)
                return Task.FromResult((TResponse)(object)(attachments ?? []));
            throw new NotImplementedException(request.GetType().Name);
        }
    }
}
