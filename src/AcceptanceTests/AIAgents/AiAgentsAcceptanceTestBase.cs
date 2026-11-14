using System.Net;
using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

/// <summary>
/// AI Agents acceptance base: API-backed seeding keyed by <see cref="AcceptanceTestBase.TestTag"/>.
/// Pool slots (<c>TestSupport</c> on) always use <c>StubChatClient</c>; tdd keeps the real model.
/// </summary>
public abstract class AiAgentsAcceptanceTestBase : AcceptanceApiTestBase
{
    /// <summary>
    /// Pool slots must report stub chat active (TestSupport forces stub); otherwise skip — never call the real model.
    /// Non-pool hosts use the real-model availability gate.
    /// </summary>
    protected static async Task SkipUnlessAiAgentChatReadyAsync()
    {
        if (await IsPoolSlotHostAsync())
        {
            if (!await IsStubChatClientActiveAsync())
            {
                Assert.Ignore(
                    "Pool-slot host does not report UseStubChatClient; " +
                    "refusing to call the real model.");
            }

            return;
        }

        await SkipIfNoChatClient();
    }

    /// <summary>True when the host exposes test-support (local acceptance host or remote pool slot).</summary>
    protected static async Task<bool> IsPoolSlotHostAsync()
    {
        var status = await AcceptanceTestSupportSettings.CreateClient().ProbeStatusAsync();
        return status != HttpStatusCode.NotFound;
    }

    /// <summary>True when test-support reports <c>UseStubChatClient</c>.</summary>
    protected static async Task<bool> IsStubChatClientActiveAsync()
    {
        var status = await AcceptanceTestSupportSettings.CreateClient().GetChatClientStatusAsync();
        return status is { UseStubChatClient: true };
    }

    /// <summary>Ignores when the host is tdd/uat/prod (no test-support); stub-only scenarios need a pool slot.</summary>
    protected static async Task RequirePoolSlotHostAsync()
    {
        if (await IsPoolSlotHostAsync() && await IsStubChatClientActiveAsync())
        {
            return;
        }

        Assert.Ignore("Requires a pool-slot host with StubChatClient (TestSupport on).");
    }

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
