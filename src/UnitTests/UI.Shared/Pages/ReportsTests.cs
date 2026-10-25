using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class ReportsTests
{
    [Test]
    public async Task Reports_OnInitialize_ShouldLoadDataViaQuery()
    {
        await using var ctx = CreateContext();
        var bus = new StubBus();
        ctx.Services.AddSingleton<IBus>(bus);

        ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        bus.SendCallCount.ShouldBe(1);
    }

    [Test]
    public async Task Reports_OnInitialize_ShouldRenderPageTitle()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        var title = component.Find($"[data-testid='{nameof(Reports.Elements.ReportsTitle)}']");
        title.TextContent.ShouldContain("Reports");
    }

    [Test]
    public async Task Reports_OnInitialize_ShouldRenderPageHeaderAndThemedExportButton()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        await component.WaitForAssertionAsync(() =>
        {
            component.FindComponents<PageHeader>().Count.ShouldBe(1);
            var button = component.Find($"[data-testid='{nameof(Reports.Elements.ExportButton)}']");
            button.ClassList.ShouldContain("btn-app");
            button.ClassList.ShouldContain("btn-app--secondary");
            component.Find($".page-actions [data-testid='{nameof(Reports.Elements.ExportButton)}']").ShouldNotBeNull();
            component.Find(".chart-bars--status").ShouldNotBeNull();
        });
    }

    [Test]
    public async Task Reports_OnInitialize_ShouldRenderPageSubtitle()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        var subtitle = component.Find($"[data-testid='{nameof(Reports.Elements.ReportsSubtitle)}']");
        subtitle.TextContent.ShouldContain("Facility work order figures in one place");
    }

    [Test]
    public async Task Reports_WithData_ShouldRenderOpenByStatusCard()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        var card = component.Find($"[data-testid='{nameof(Reports.Elements.SummaryCardStatus)}']");
        card.ShouldNotBeNull();
        card.TextContent.ShouldContain("Open by Status");
        card.TextContent.ShouldContain("Draft");
        card.TextContent.ShouldContain("Assigned");
        card.TextContent.ShouldContain("In Progress");
    }

    [Test]
    public async Task Reports_WithData_ShouldRenderOpenByPriorityCard()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        var card = component.Find($"[data-testid='{nameof(Reports.Elements.SummaryCardPriority)}']");
        card.ShouldNotBeNull();
        card.TextContent.ShouldContain("Open by Priority");
        card.TextContent.ShouldContain("High");
        card.TextContent.ShouldContain("Medium");
        card.TextContent.ShouldContain("Low");
    }

    [Test]
    public async Task Reports_WithData_ShouldRenderOpenByBuildingCard()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        var card = component.Find($"[data-testid='{nameof(Reports.Elements.SummaryCardBuilding)}']");
        card.ShouldNotBeNull();
        card.TextContent.ShouldContain("Open by Building");
        card.TextContent.ShouldContain("Sanctuary");
        card.TextContent.ShouldContain("Kitchen");
    }

    [Test]
    public async Task Reports_WithData_ShouldRenderCompletedLast30Card()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        var card = component.Find($"[data-testid='{nameof(Reports.Elements.SummaryCardCompletedLast30)}']");
        card.ShouldNotBeNull();
        card.TextContent.ShouldContain("Completed (30 days)");
        card.TextContent.ShouldContain("Avg.");
        card.TextContent.ShouldContain("days to close");
    }

    [Test]
    public async Task Reports_WithData_ShouldRenderCompletedPerWeekChart()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        var chart = component.Find($"[data-testid='{nameof(Reports.Elements.ChartCompletedPerWeek)}']");
        chart.ShouldNotBeNull();
        chart.TextContent.ShouldContain("Completed per week");
        chart.QuerySelector(".weekly-chart-html").ShouldNotBeNull();
    }

    [Test]
    public async Task Reports_WithData_ShouldRenderOverdueKpi()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        var kpi = component.Find($"[data-testid='{nameof(Reports.Elements.KpiOverdue)}']");
        kpi.ShouldNotBeNull();
        kpi.TextContent.ShouldContain("Overdue now");
        kpi.TextContent.ShouldContain("action needed");
    }

    [Test]
    public async Task Reports_WithData_ShouldEnableExportButton()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        var button = component.Find($"[data-testid='{nameof(Reports.Elements.ExportButton)}']");
        button.GetAttribute("disabled").ShouldBeNull();
    }

    [Test]
    public async Task Reports_ExportButton_ShouldInvokeCsvDownloadModule()
    {
        await using var ctx = CreateContext();
        var module = ctx.JSInterop.SetupModule("./_content/ClearMeasure.Bootcamp.UI.Shared/js/searchExport.js");
        module.SetupVoid("downloadCsv", _ => true).SetVoidResult();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        await component.Find($"[data-testid='{nameof(Reports.Elements.ExportButton)}']").ClickAsync(new());

        var invocation = module.VerifyInvoke("downloadCsv");
        invocation.Arguments[0]!.ToString()!.ShouldEndWith(".csv");
        invocation.Arguments[1]!.ToString()!.ShouldContain("Overdue Count,1");
    }

    [Test]
    public async Task Reports_WithoutData_ShouldDisableExportButton()
    {
        await using var ctx = CreateContext();
        var bus = new StubBusReturningNull();
        ctx.Services.AddSingleton<IBus>(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        var button = component.Find($"[data-testid='{nameof(Reports.Elements.ExportButton)}']");
        button.GetAttribute("disabled").ShouldNotBeNull();
    }

    [Test]
    public async Task Reports_StatusCounts_ShouldMatchFixedData()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        var card = component.Find($"[data-testid='{nameof(Reports.Elements.SummaryCardStatus)}']");
        // Verify counts appear in the card (2, 3, 1 from stub data)
        card.TextContent.ShouldContain("2");
        card.TextContent.ShouldContain("3");
    }

    [Test]
    public async Task Reports_PriorityCounts_ShouldMatchFixedData()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        var card = component.Find($"[data-testid='{nameof(Reports.Elements.SummaryCardPriority)}']");
        card.TextContent.ShouldContain("2");
        card.TextContent.ShouldContain("3");
        card.TextContent.ShouldContain("1");
    }

    [Test]
    public async Task Reports_AverageDaysToClose_ShouldDisplayCorrectValue()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        var card = component.Find($"[data-testid='{nameof(Reports.Elements.SummaryCardCompletedLast30)}']");
        // Stub returns 2.5
        card.TextContent.ShouldContain("2.5");
    }

    [Test]
    public async Task Reports_OverdueCount_ShouldDisplayCorrectValue()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<Reports>());

        var kpi = component.Find($"[data-testid='{nameof(Reports.Elements.KpiOverdue)}']");
        // Stub returns 1
        kpi.TextContent.ShouldContain("1");
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var store = new StubUserSessionStore { Username = "testuser" };
        var authProvider = new CustomAuthenticationStateProvider(store);
        authProvider.Login("testuser").GetAwaiter().GetResult();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(authProvider);

        return ctx;
    }

    private class StubBusReturningNull() : Bus(null!)
    {

        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderReportsQuery)
            {
                return Task.FromResult((TResponse?)(object?)null)!;
            }

            throw new NotImplementedException();
        }
    }
}
