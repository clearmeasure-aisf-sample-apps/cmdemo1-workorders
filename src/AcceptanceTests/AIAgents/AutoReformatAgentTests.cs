using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

/// <summary>
/// Acceptance test for the AutoReformatAgentService.
/// Pool slots use the stub model; do not point slots at the real model.
/// </summary>
[TestFixture]
public class AutoReformatAgentTests : AiAgentsAcceptanceTestBase
{
    [Test, LlmTest, Explicit]
    public async Task ShouldReformatWorkOrderTitleAndDescription()
    {
        await LoginAsCurrentUser();
        var order = await SeedDraftForReformatAsync();
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description!);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");

        var rehydrated = await WaitForReformatAsync(order);
        rehydrated.ShouldNotBeNull();
        rehydrated.Title.ShouldNotBeNull();
        rehydrated.Title![0].ShouldBe(char.ToUpper(rehydrated.Title[0]));
        rehydrated.Description.ShouldNotBeNull();
        rehydrated.Description.ShouldNotBe(order.Description);
    }

    private async Task<WorkOrder?> WaitForReformatAsync(WorkOrder order)
    {
        WorkOrder? rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        for (var attempt = 0; attempt < 120 && rehydrated?.Description == order.Description; attempt++)
        {
            await Task.Delay(250);
// Bounded poll of one work order until the reformat agent writes. Same key each attempt, not a per-row fan-out.
#pragma warning disable CM0001 // Bounded poll of one work order until the reformat agent writes
            rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
#pragma warning restore CM0001
        }

        return rehydrated;
    }
}
