using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using Bunit;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class WorkOrderChatAutocompleteTests
{
    [Test]
    public async Task ShouldTurnOffAutofillOnTheChatInput()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddLogging();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.Render<WorkOrderChat>();

        var workOrder = new WorkOrder
        {
            Number = "WO-005",
            Assignee = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com")
        };
        await component.InvokeAsync(() => component.Instance.Handle(new WorkOrderSelectedEvent(workOrder)));
        component.Render();

        var input = component.Find($"[data-testid='{nameof(WorkOrderChat.Elements.ChatInput)}']");
        input.GetAttribute("autocomplete").ShouldBe("off");
    }
}
