using Bunit;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class NotificationsPanelEmptyStateTests
{
    private static void SetupClockAndJs(BunitContext ctx)
    {
        ctx.Services.AddSingleton(new ChurchCalendar(TimeProvider.System));
        var module = ctx.JSInterop.SetupModule(NotificationsPanelScript.ModulePath);
        module.SetupVoid("setupEscapeHandler", _ => true);
    }

    [Test]
    public void ShouldRenderEmptyStatesWithStatusRole_WhenAllSectionsAreEmpty()
    {
        using var ctx = new BunitContext();
        SetupClockAndJs(ctx);

        var component = ctx.Render<NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, new NotificationsSummary([], [], [])));

        var paragraphs = component.FindAll("p.empty-state");
        paragraphs.Count.ShouldBe(3);
        paragraphs[0].GetAttribute("role").ShouldBe("status");
        paragraphs[1].GetAttribute("role").ShouldBe("status");
        paragraphs[2].GetAttribute("role").ShouldBe("status");
    }
}
