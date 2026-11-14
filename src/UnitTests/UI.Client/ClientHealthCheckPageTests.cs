using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Client.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class ClientHealthCheckPageTests
{
    [Test]
    public async Task ClientHealthCheck_ShouldRenderStatus_WhenHealthServiceReturnsReport()
    {
        await using var ctx = CreateContextWithHealthChecks();

        var component = ctx.Render<ClientHealthCheck>();

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{ClientHealthCheck.Elements.Status}']").ShouldNotBeNull();
        });
        component.Markup.ShouldNotContain("Loading health report");
    }

    [Test]
    public async Task DetailedClientHealthCheck_ShouldRenderStatus_WhenHealthServiceReturnsReport()
    {
        await using var ctx = CreateContextWithHealthChecks();

        var component = ctx.Render<DetailedClientHealthCheck>();

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{DetailedClientHealthCheck.Elements.Status}']").ShouldNotBeNull();
        });
        component.Markup.ShouldNotContain("Loading detailed health report");
    }

    [Test]
    public async Task DetailedClientHealthCheck_ShouldNameDiagnosticTableAndScopeHeaders_WhenEntryHasData()
    {
        const string entryKey = "sample-check";
        await using var ctx = CreateContextWithHealthChecks(builder =>
            builder.AddCheck(entryKey, () => HealthCheckResult.Healthy(
                data: new Dictionary<string, object> { ["probe"] = "ok" })));

        var component = ctx.Render<DetailedClientHealthCheck>();

        await component.WaitForAssertionAsync(() =>
        {
            component.Find("table.data-table").ShouldNotBeNull();
        });
        var table = component.Find("table.data-table");
        table.GetAttribute("aria-label").ShouldBe($"Diagnostic data for {entryKey}");
        component.FindAll("table.data-table th").ShouldAllBe(th => th.GetAttribute("scope") == "col");
        component.FindAll("table.data-table th").Count.ShouldBe(2);
    }

    private static BunitContext CreateContextWithHealthChecks(
        Action<IHealthChecksBuilder>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddHealthChecks();
        configure?.Invoke(builder);
        var provider = services.BuildServiceProvider();

        var ctx = new BunitContext();
        ctx.Services.AddSingleton(provider.GetRequiredService<HealthCheckService>());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        return ctx;
    }
}
