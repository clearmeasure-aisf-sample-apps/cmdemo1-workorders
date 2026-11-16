using Bunit;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class NotificationsPanelEmptyStateTests
{
    private static void SetupClockAndJs(BunitContext ctx)
    {
        ctx.Services.AddSingleton(TimeProvider.System);
        var module = ctx.JSInterop.SetupModule(NotificationsPanelScript.ModulePath);
        module.SetupVoid("setupEscapeHandler", _ => true);
    }

    [Test]
    public void ShouldAnnounceEmptyMessagesWithRoleStatus()
    {
        using var ctx = new BunitContext();
        SetupClockAndJs(ctx);

        var component = ctx.Render<NotificationsPanel>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Data, new NotificationsSummary([], [], [])));

        var emptyStates = component.FindAll("p.empty-state");
        emptyStates.Count.ShouldBe(3);
        foreach (var element in emptyStates)
        {
            element.GetAttribute("role").ShouldBe("status");
        }
    }
}
