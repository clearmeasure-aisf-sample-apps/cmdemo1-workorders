using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class SlaHealthCardTests
{
    [Test]
    public void ShouldRenderLoadingState_WhenInitializing()
    {
        var ctx = CreateContext();
        var component = ctx.Render<SlaHealthCard>();

        component.Find($"[data-testid='{SlaHealthCard.Elements.SlaHealthCard}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldRenderNoData_When_NoQualifyingOrders()
    {
        var ctx = CreateContext(new SlaHealth { CurrentMonthPercentage = null });
        var component = ctx.Render<SlaHealthCard>();

        // Wait for component to initialize
        await Task.Delay(100);

        var noDataElement = component.Find(".sla-health-no-data");
        noDataElement.ShouldNotBeNull();
        noDataElement.TextContent.ShouldBe("No data");
    }

    [Test]
    public async Task ShouldRenderPercentage_WhenDataAvailable()
    {
        var health = new SlaHealth
        {
            CurrentMonthPercentage = 85m,
            LastMonthPercentage = 75m,
            CurrentMonthOrderCount = 20,
            LastMonthOrderCount = 20
        };

        var ctx = CreateContext(health);
        var component = ctx.Render<SlaHealthCard>();

        await Task.Delay(100);

        var valueElement = component.Find(".sla-health-value");
        valueElement.ShouldNotBeNull();
        valueElement.TextContent.ShouldContain("85%");
    }

    [Test]
    public async Task ShouldRenderTrendArrow_AndTextLabel()
    {
        var health = new SlaHealth
        {
            CurrentMonthPercentage = 90m,
            LastMonthPercentage = 75m,
            CurrentMonthOrderCount = 20,
            LastMonthOrderCount = 20
        };

        var ctx = CreateContext(health);
        var component = ctx.Render<SlaHealthCard>();

        await Task.Delay(100);

        var trendElement = component.Find(".sla-health-trend");
        trendElement.ShouldNotBeNull();
        // Text label, not color only
        trendElement.TextContent.ShouldBe("↑ up from 75%");
    }

    [Test]
    public async Task ShouldRenderProgressBar_With_AriaValueNow()
    {
        var health = new SlaHealth
        {
            CurrentMonthPercentage = 75m,
            LastMonthPercentage = 75m,
            CurrentMonthOrderCount = 20,
            LastMonthOrderCount = 20
        };

        var ctx = CreateContext(health);
        var component = ctx.Render<SlaHealthCard>();

        await Task.Delay(100);

        var progressBar = component.Find("[role='progressbar']");
        progressBar.ShouldNotBeNull();
        progressBar.GetAttribute("aria-valuenow")!.ShouldBe("75");
        progressBar.GetAttribute("aria-valuemin")!.ShouldBe("0");
        progressBar.GetAttribute("aria-valuemax")!.ShouldBe("100");
    }

    [Test]
    public async Task ShouldRenderOrderCount()
    {
        var health = new SlaHealth
        {
            CurrentMonthPercentage = 80m,
            LastMonthPercentage = 80m,
            CurrentMonthOrderCount = 5,
            LastMonthOrderCount = 5
        };

        var ctx = CreateContext(health);
        var component = ctx.Render<SlaHealthCard>();

        await Task.Delay(100);

        var subtitle = component.Find(".sla-health-subtitle");
        subtitle.ShouldNotBeNull();
        subtitle.TextContent.ShouldBe("5 work orders");
    }

    [Test]
    public async Task ShouldPluralizeWorkOrderCount()
    {
        var health = new SlaHealth
        {
            CurrentMonthPercentage = 80m,
            LastMonthPercentage = 80m,
            CurrentMonthOrderCount = 1,
            LastMonthOrderCount = 1
        };

        var ctx = CreateContext(health);
        var component = ctx.Render<SlaHealthCard>();

        await Task.Delay(100);

        var subtitle = component.Find(".sla-health-subtitle");
        subtitle.TextContent.ShouldBe("1 work order");
    }

    [Test]
    public void ShouldHandleWorkOrderChangedEvent()
    {
        var health = new SlaHealth
        {
            CurrentMonthPercentage = 80m,
            LastMonthPercentage = 80m,
            CurrentMonthOrderCount = 10,
            LastMonthOrderCount = 10
        };

        var ctx = CreateContext(health);
        var component = ctx.Render<SlaHealthCard>();

        var cardInstance = component.Instance;

        var workOrder = new WorkOrder
        {
            Number = "WO-001",
            Title = "Test work order",
            Status = WorkOrderStatus.Complete
        };

        var @event = new WorkOrderChangedEvent(new StateCommandResult(workOrder));

        // Should not throw
        cardInstance.Handle(@event);
    }

    [Test]
    public async Task ShouldShowTrendDown()
    {
        var health = new SlaHealth
        {
            CurrentMonthPercentage = 60m,
            LastMonthPercentage = 80m,
            CurrentMonthOrderCount = 20,
            LastMonthOrderCount = 20
        };

        var ctx = CreateContext(health);
        var component = ctx.Render<SlaHealthCard>();

        await Task.Delay(100);

        var trendElement = component.Find(".sla-health-trend");
        trendElement.TextContent.ShouldBe("↓ down from 80%");
    }

    [Test]
    public async Task ShouldShowTrendFlat()
    {
        var health = new SlaHealth
        {
            CurrentMonthPercentage = 75m,
            LastMonthPercentage = 75m,
            CurrentMonthOrderCount = 20,
            LastMonthOrderCount = 20
        };

        var ctx = CreateContext(health);
        var component = ctx.Render<SlaHealthCard>();

        await Task.Delay(100);

        var trendElement = component.Find(".sla-health-trend");
        trendElement.TextContent.ShouldBe("— flat at 75%");
    }

    private static BunitContext CreateContext(SlaHealth? health = null)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubSlaHealthBus(health));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        return ctx;
    }

    private class StubSlaHealthBus(SlaHealth? health = null) : Bus(null!)
    {
        private readonly SlaHealth _health = health ?? new SlaHealth
        {
            CurrentMonthPercentage = null,
            LastMonthPercentage = null,
            CurrentMonthOrderCount = 0,
            LastMonthOrderCount = 0
        };

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is SlaHealthQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)_health);
            }

            throw new NotImplementedException();
        }
    }
}
