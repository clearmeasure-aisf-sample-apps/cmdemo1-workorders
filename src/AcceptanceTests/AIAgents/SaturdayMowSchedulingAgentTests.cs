using System.Globalization;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Login = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;
using Microsoft.EntityFrameworkCore;
using NavMenu = ClearMeasure.Bootcamp.UI.Shared.NavMenu;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

/// <summary>
/// Required LLM acceptance: Lovejoy schedules ten Saturday mows for Willie via AI Agent.
/// Skips when no chat client is configured.
/// </summary>
[TestFixture]
public class SaturdayMowSchedulingAgentTests : AcceptanceTestBase
{
    [SetUp]
    public async Task EnsureLlmAvailable()
    {
        await SkipIfNoChatClient();
    }

    [Test]
    [LlmTest]
    public async Task ShouldCreateTenSaturdayMowsForWillieViaAiAgent()
    {
        await Page.GotoAsync("/login");
        var shortcut = Page.GetByTestId(nameof(Login.Elements.LovejoyShortcut));
        await shortcut.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000
        });
        await Click(nameof(Login.Elements.LovejoyShortcut));
        await Expect(Page.GetByTestId(nameof(Logout.Elements.WelcomeText)))
            .ToHaveTextAsync("Welcome tlovejoy!");

        await Click(nameof(NavMenu.Elements.AiAgent));
        await Page.WaitForURLAsync("**/ai-agent");

        const string prompt =
            "Create 10 work orders for Groundskeeper Willie MacDougal to mow the grass, " +
            "one per week for the next 10 Saturdays, each due that Saturday (the day before Sunday service). " +
            "In your reply list every work order number and due date.";

        // Work orders that exist before the chat, so only the ones the agent creates are counted.
        HashSet<Guid> before;
        await using (var snapshot = TestHost.NewDbContext())
        {
            before = (await snapshot.Set<WorkOrder>().Select(w => w.Id).ToListAsync()).ToHashSet();
        }

        await Input(nameof(ApplicationChat.Elements.ChatInput), prompt);
        await Click(nameof(ApplicationChat.Elements.SendButton));

        var expectedDates = WorkOrderToolsComingSaturdays();
        var firstDateText = expectedDates[0].ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        // Wait for the assistant bubble to contain tool-backed content (not merely to exist).
        // AiMessage1 can appear before the full reply is meaningful if the model echoes early.
        var aiMessage = Page.GetByTestId(nameof(ApplicationChat.Elements.AiMessage) + "1");
        await Expect(aiMessage).ToContainTextAsync(
            firstDateText,
            new LocatorAssertionsToContainTextOptions { Timeout = 180_000 });
        await Expect(Page.GetByTestId(nameof(ApplicationChat.Elements.LoadingIndicator)))
            .ToHaveCountAsync(0);

        var chatText = await Page.GetByTestId(nameof(ApplicationChat.Elements.ChatHistory)).InnerTextAsync();
        chatText.ShouldNotBeNullOrEmpty();
        var aiText = await aiMessage.InnerTextAsync();
        aiText.ShouldNotBeNullOrEmpty();

        foreach (var date in expectedDates)
        {
            var iso = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            aiText.ShouldContain(iso);
            chatText.ShouldContain(iso);
        }

        await using var fresh = TestHost.NewDbContext();
        var dueOrders = await fresh.Set<WorkOrder>().AsNoTracking()
            .Include(wo => wo.Creator)
            .Include(wo => wo.Assignee)
            .Where(wo => wo.DueDate != null)
            .ToListAsync();
        var created = dueOrders
            .Where(wo => !before.Contains(wo.Id)
                         && wo.Creator?.UserName == "tlovejoy"
                         && wo.Assignee?.UserName == "gwillie"
                         && wo.DueDate != null
                         && expectedDates.Contains(wo.DueDate.Value))
            .OrderBy(wo => wo.DueDate)
            .ToList();

        created.Count.ShouldBeGreaterThanOrEqualTo(10,
            $"Expected at least 10 Saturday mows for gwillie. Chat: {chatText}");

        var matchingSet = created
            .GroupBy(wo => wo.DueDate)
            .Select(g => g.First())
            .OrderBy(wo => wo.DueDate)
            .Take(10)
            .ToList();
        matchingSet.Count.ShouldBe(10);
        matchingSet.Select(wo => wo.DueDate!.Value).ShouldBe(expectedDates);

        foreach (var wo in matchingSet)
        {
            chatText.ShouldContain(wo.Number!);
        }

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search**");
        // Full Page.GotoAsync drops in-memory Blazor auth. Filter within the SPA instead.
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        await Expect(assigneeSelect).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await assigneeSelect.SelectOptionAsync("gwillie");

        foreach (var wo in matchingSet)
        {
            var cell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + wo.Number);
            await Expect(cell).ToBeAttachedAsync(new LocatorAssertionsToBeAttachedOptions { Timeout = 30_000 });
            await Expect(cell).ToContainTextAsync(
                wo.DueDate!.Value.ToString("MMM d, yyyy", CultureInfo.InvariantCulture));
        }
    }

    private static IReadOnlyList<DateOnly> WorkOrderToolsComingSaturdays()
    {
        var first = ChurchTimeZone.ComingSaturday(TimeProvider.System);
        return Enumerable.Range(0, 10).Select(i => first.AddDays(7 * i)).ToList();
    }
}
