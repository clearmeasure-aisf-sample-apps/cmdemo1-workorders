using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class WorkOrderStatusHistoryEmptyStatusTests
{
    [Test]
    public async Task ShouldAnnounceEmptyStatusHistoryWithRoleStatus()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.Render<WorkOrderStatusHistory>(p => p.Add(c => c.Changes, Array.Empty<WorkOrderStatusChange>()));

        var emptyMessage = component.Find($"[data-testid='{WorkOrderManage.Elements.StatusHistory}'][role='status']");

        emptyMessage.TextContent.Trim().ShouldBe("No status changes recorded.");
    }
}
