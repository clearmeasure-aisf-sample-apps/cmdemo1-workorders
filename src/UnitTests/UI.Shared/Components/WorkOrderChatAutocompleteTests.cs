using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class WorkOrderChatAutocompleteTests
{
    [Test]
    public async Task ChatInputTurnsAutocompleteOff()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses();
        var chat = ctx.Render<ClearMeasure.Bootcamp.UI.Shared.Components.WorkOrderChat>();
        var assigned = new WorkOrder { Number = "WO-777", Assignee = new Employee("hsimpson", "Homer", "Simpson", "homer@example.org") };
        await chat.InvokeAsync(() => chat.Instance.Handle(new WorkOrderSelectedEvent(assigned)));
        chat.Find("input#work-order-chat-input").GetAttribute("autocomplete").ShouldBe("off");
    }
}
