using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class SlaHealthCardNoDataStatusTests
{
    [Test]
    public async Task NoDataElementShouldHaveRoleStatus()
    {
        var ctx = CreateContext(new SlaHealth { CurrentMonthPercentage = null });
        var component = ctx.Render<SlaHealthCard>();

        await Task.Delay(100);

        var noDataElement = component.Find($"[data-testid='{SlaHealthCard.Elements.SlaHealthNoData}']");
        noDataElement.ShouldNotBeNull();
        noDataElement.GetAttribute("role").ShouldBe("status");
        noDataElement.TextContent.ShouldBe("No data");
    }

    private static BunitContext CreateContext(SlaHealth health)
    {
        var ctx = new BunitContext();
        ctx.Services.AddLogging();
        ctx.Services.AddSingleton<IBus>(new StubSlaHealthBus(health));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        return ctx;
    }

    private class StubSlaHealthBus(SlaHealth health) : Bus(null!)
    {
        private readonly SlaHealth _health = health;

        public override async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is SlaHealthQuery)
            {
                return (TResponse)(object)_health;
            }

            throw new NotImplementedException();
        }
    }
}
