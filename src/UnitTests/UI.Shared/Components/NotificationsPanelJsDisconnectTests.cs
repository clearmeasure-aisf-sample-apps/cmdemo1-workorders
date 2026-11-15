using Bunit;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class NotificationsPanelJsDisconnectTests
{
    [Test]
    public async Task ShouldSurvive_WhenModuleImportDisconnectsDuringInit()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IJSRuntime>(new DisconnectingJsRuntime(failImport: true));

        var component = ctx.Render<NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, new NotificationsSummary([], [], [])));

        component.Find(".notifications-panel").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldSurvive_WhenModuleDisposeDisconnects()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IJSRuntime>(new DisconnectingJsRuntime(failDispose: true));
        ctx.Render<NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, new NotificationsSummary([], [], [])));

        await Should.NotThrowAsync(async () => await ctx.DisposeAsync());
    }
}
