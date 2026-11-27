using Bunit;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderPrintFloorTests
{
    [Test]
    public async Task ShouldShowFloor_WhenFloorIsSet()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IBus>(new FloorBus("WO-1", "2"));
        ctx.Services.AddSingleton(new ChurchCalendar(TimeProvider.System));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var c = ctx.Render<WorkOrderPrint>(p => p.Add(x => x.Number, "WO-1"));

        var floor = c.Find("[data-testid='Floor']");
        floor.TextContent.ShouldBe("2");
    }

    [Test]
    public async Task ShouldShowNone_WhenFloorIsBlank()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IBus>(new FloorBus("WO-1", null));
        ctx.Services.AddSingleton(new ChurchCalendar(TimeProvider.System));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var c = ctx.Render<WorkOrderPrint>(p => p.Add(x => x.Number, "WO-1"));

        var floor = c.Find("[data-testid='Floor']");
        floor.TextContent.ShouldBe("None");
    }

    private sealed class FloorBus : Bus
    {
        private readonly WorkOrder _workOrder;

        public FloorBus(string number, string? floor) : base(null!)
        {
            _workOrder = WorkOrderBuilder.Create().WithNumber(number).Build();
            _workOrder.Floor = floor;
        }

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult((TResponse)(object)_workOrder);
            }
            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }
            return Task.FromResult((TResponse)(object)null!);
        }
    }
}
