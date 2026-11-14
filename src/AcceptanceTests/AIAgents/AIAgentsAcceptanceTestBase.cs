using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

/// <summary>
/// AI Agents acceptance base: API-backed seeding keyed by <see cref="AcceptanceTestBase.TestTag"/>.
/// Pool slots use the stub model (item 22); do not point slots at the real model.
/// </summary>
public abstract class AIAgentsAcceptanceTestBase : AcceptanceApiTestBase
{
    /// <summary>Opens the AI Agent page after sign-in.</summary>
    protected async Task OpenAiAgentPageAsync()
    {
        await Click(nameof(UI.Shared.NavMenu.Elements.AiAgent));
        await Page.WaitForURLAsync("**/ai-agent");
    }

    /// <summary>Sends <paramref name="prompt"/> and waits for the first assistant bubble.</summary>
    protected async Task SendChatPromptAsync(string prompt, float timeoutMs = 120_000)
    {
        await Input(nameof(ApplicationChat.Elements.ChatInput), prompt);
        await Click(nameof(ApplicationChat.Elements.SendButton));
        var aiMessage = Page.GetByTestId(nameof(ApplicationChat.Elements.AiMessage) + "1");
        await aiMessage.WaitForAsync(new LocatorWaitForOptions { Timeout = timeoutMs });
    }

    /// <summary>Work-order ids matching <paramref name="assignee"/> before the agent runs.</summary>
    protected async Task<HashSet<Guid>> SnapshotAssigneeOrderIdsAsync(Employee assignee)
    {
        var query = new WorkOrderSpecificationQuery();
        query.MatchAssignee(assignee);
        var existing = await Bus.Send(query);
        return existing.Select(wo => wo.Id).ToHashSet();
    }

    /// <summary>Orders for <paramref name="assignee"/> whose title contains <see cref="AcceptanceTestBase.TestTag"/>.</summary>
    protected async Task<IReadOnlyList<WorkOrder>> LoadTaggedAssigneeOrdersAsync(Employee assignee)
    {
        var query = new WorkOrderSpecificationQuery();
        query.MatchAssignee(assignee);
        query.MatchSearchText(TestTag);
        return await Bus.Send(query);
    }

    /// <summary>Seeds a draft titled with <see cref="AcceptanceTestBase.TestTag"/> for reformatting.</summary>
    protected Task<WorkOrder> SeedDraftForReformatAsync() =>
        SeedWorkOrderAsync(CurrentUser, WorkOrderStatus.Draft, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] lowercase needs fixing",
            Description = "this is bad grammer and no punctuation missing capital letters"
        });
}
