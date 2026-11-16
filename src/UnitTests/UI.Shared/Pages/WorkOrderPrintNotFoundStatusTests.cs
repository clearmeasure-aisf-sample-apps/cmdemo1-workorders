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
public class WorkOrderPrintNotFoundStatusTests
{
    [Test]
    public async Task ShouldAnnounceNotFoundMessageWithRoleStatus()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IBus>(new NullBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var c = ctx.Render<WorkOrderPrint>(p => p.Add(x => x.Number, "NOPE"));

        c.Find("[data-testid='" + WorkOrderPrint.Elements.NotFound + "']")
            .GetAttribute("role").ShouldBe("status");
    }

    private class NullBus() : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request) =>
            Task.FromResult((TResponse)(object)null!);
    }
}
