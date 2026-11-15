using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class WorkOrderSearchTableJsDisconnectTests
{
    [Test]
    public async Task ShouldSurvive_WhenSetIndeterminateDisconnects()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IJSRuntime>(new DisconnectingJsRuntime(failImport: true));

        var table = ctx.Render<WorkOrderSearchTable>(p => p
            .Add(c => c.Rows, SearchResultRowMother.Numbered("501"))
            .Add(c => c.Selection, new WorkOrderRowSelection()));

        table.Find("table.grid-data").ShouldNotBeNull();
    }
}
