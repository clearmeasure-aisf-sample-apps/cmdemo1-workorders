using Bunit;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderPrintTests
{
    private static string Id(WorkOrderPrint.Elements e) => $"[data-testid='{e}']";

    [Test]
    public async Task ShouldRenderAllFields()
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

        c.Find(Id(WorkOrderPrint.Elements.Number)).TextContent.ShouldBe("WO-1");
        c.Find(Id(WorkOrderPrint.Elements.Title)).TextContent.ShouldBe("Leaky roof");
        c.Find(Id(WorkOrderPrint.Elements.Status)).TextContent.ShouldBe(wo.FriendlyStatus);
        c.Find(Id(WorkOrderPrint.Elements.Priority)).TextContent.ShouldBe("High");
        c.Find(Id(WorkOrderPrint.Elements.Building)).TextContent.ShouldBe("Sanctuary");
        c.Find(Id(WorkOrderPrint.Elements.RoomNumber)).TextContent.ShouldBe("12");
        c.Find(Id(WorkOrderPrint.Elements.Creator)).TextContent.ShouldBe("Ann Lee");
        c.Find(Id(WorkOrderPrint.Elements.Assignee)).TextContent.ShouldBe("Bob Ray");
        c.Find(Id(WorkOrderPrint.Elements.CreatedDate)).TextContent.ShouldBe("2025-01-15 12:30 PM");
        c.Find(Id(WorkOrderPrint.Elements.AssignedDate)).TextContent.ShouldBe("None");
        c.Find(Id(WorkOrderPrint.Elements.CompletedDate)).TextContent.ShouldBe("None");
        c.Find(Id(WorkOrderPrint.Elements.DueDate)).TextContent.ShouldBe("None");
        c.Find(Id(WorkOrderPrint.Elements.Description)).TextContent.ShouldBe("Drips");
        c.Find(Id(WorkOrderPrint.Elements.Instructions)).TextContent.ShouldBe("Bucket");
        c.Find(Id(WorkOrderPrint.Elements.AttachmentFileName)).TextContent.ShouldBe("roof.jpg");
    }

    [Test]
    public async Task ShouldShowNoneForEmptyValues()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var wo = new WorkOrder { Number = "WO-2", Title = "", Description = "", Instructions = "" };
        ctx.Services.AddSingleton<IBus>(new StubBus(wo));
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var c = ctx.Render<WorkOrderPrint>(p => p.Add(x => x.Number, "WO-2"));

        foreach (var e in new[] { WorkOrderPrint.Elements.Title, WorkOrderPrint.Elements.Building,
                     WorkOrderPrint.Elements.RoomNumber, WorkOrderPrint.Elements.Creator,
                     WorkOrderPrint.Elements.Assignee, WorkOrderPrint.Elements.Description,
                     WorkOrderPrint.Elements.Instructions, WorkOrderPrint.Elements.AttachmentsNone })
            c.Find(Id(e)).TextContent.ShouldBe("None");
    }

    [Test]
    public async Task ShouldShowDueDateWithUrgencyText()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var wo = new WorkOrder { Number = "WO-3", Title = "Late", Status = WorkOrderStatus.Assigned, DueDate = today.AddDays(-1) };
        ctx.Services.AddSingleton<IBus>(new StubBus(wo));
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var c = ctx.Render<WorkOrderPrint>(p => p.Add(x => x.Number, "WO-3"));

        c.Find(Id(WorkOrderPrint.Elements.DueDate)).TextContent
            .ShouldBe($"{today.AddDays(-1):yyyy-MM-dd} (Overdue)");
    }

    [Test]
    public async Task ShouldShowDueDateWithoutUrgencyText()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var due = ChurchTimeZone.Today(TimeProvider.System).AddDays(10);
        var wo = new WorkOrder { Number = "WO-4", Title = "Later", Status = WorkOrderStatus.Assigned, DueDate = due };
        ctx.Services.AddSingleton<IBus>(new StubBus(wo));
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var c = ctx.Render<WorkOrderPrint>(p => p.Add(x => x.Number, "WO-4"));

        c.Find(Id(WorkOrderPrint.Elements.DueDate)).TextContent.ShouldBe(due.ToString("yyyy-MM-dd"));
    }

    [Test]
    public async Task ShouldShowNotFoundForUnknownNumber()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IBus>(new StubBus(null));
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var c = ctx.Render<WorkOrderPrint>(p => p.Add(x => x.Number, "NOPE"));

        c.Find(Id(WorkOrderPrint.Elements.NotFound)).TextContent.ShouldBe("Work order not found");
        c.FindAll(Id(WorkOrderPrint.Elements.Title)).Count.ShouldBe(0);
    }

    [TestCase("DOESNOTEXIST")]
    [TestCase(" ")]
    public async Task ShouldShowNotFoundWhenLookupThrows(string number)
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IBus>(new ThrowingBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var c = ctx.Render<WorkOrderPrint>(p => p.Add(x => x.Number, number));

        c.Find(Id(WorkOrderPrint.Elements.NotFound)).TextContent.ShouldBe("Work order not found");
    }

    private class ThrowingBus() : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request) =>
            throw new InvalidOperationException();
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
