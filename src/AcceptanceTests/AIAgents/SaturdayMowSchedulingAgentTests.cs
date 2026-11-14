using System.Globalization;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

/// <summary>
/// Required LLM acceptance: schedules ten Saturday mows via AI Agent for a TestTag-scoped assignee.
/// Pool slots use the stub model; do not point slots at the real model. Skips when no chat client.
/// </summary>
[TestFixture]
public class SaturdayMowSchedulingAgentTests : AiAgentsAcceptanceTestBase
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
        var assignee = await SeedEmployeeAsync();
        await LoginAsCurrentUser();
        await OpenAiAgentPageAsync();

        var expectedDates = ComingSaturdays();
        var firstDateText = expectedDates[0].ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var prompt =
            $"Create 10 work orders for {assignee.UserName} to mow the grass, " +
            "one per week for the next 10 Saturdays, each due that Saturday. " +
            $"Put [{TestTag}] in every title. In your reply list every work order number and due date.";

        await Input(nameof(ApplicationChat.Elements.ChatInput), prompt);
        await Click(nameof(ApplicationChat.Elements.SendButton));
        await Expect(Page.GetByTestId(nameof(ApplicationChat.Elements.AiMessage) + "1"))
            .ToContainTextAsync(firstDateText, new LocatorAssertionsToContainTextOptions { Timeout = 180_000 });
        await Expect(Page.GetByTestId(nameof(ApplicationChat.Elements.LoadingIndicator))).ToHaveCountAsync(0);

        var chatText = await Page.GetByTestId(nameof(ApplicationChat.Elements.ChatHistory)).InnerTextAsync();
        await AssertSaturdayMowsAsync(assignee, expectedDates, chatText);
    }

    private async Task AssertSaturdayMowsAsync(
        Employee assignee,
        IReadOnlyList<DateOnly> expectedDates,
        string chatText)
    {
        chatText.ShouldNotBeNullOrEmpty();
        var aiText = await Page.GetByTestId(nameof(ApplicationChat.Elements.AiMessage) + "1").InnerTextAsync();
        foreach (var date in expectedDates)
        {
            var iso = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            aiText.ShouldContain(iso);
            chatText.ShouldContain(iso);
        }

        var matching = (await LoadTaggedAssigneeOrdersAsync(assignee))
            .Where(wo => wo.DueDate != null && expectedDates.Contains(wo.DueDate.Value))
            .GroupBy(wo => wo.DueDate)
            .Select(g => g.First())
            .OrderBy(wo => wo.DueDate)
            .Take(10)
            .ToList();
        matching.Count.ShouldBe(10, $"Expected 10 Saturday mows. Chat: {chatText}");
        matching.Select(wo => wo.DueDate!.Value).ShouldBe(expectedDates);
        foreach (var wo in matching)
        {
            chatText.ShouldContain(wo.Number!);
        }

        await AssertSearchShowsDueDatesAsync(assignee, matching);
    }

    private async Task AssertSearchShowsDueDatesAsync(Employee assignee, IReadOnlyList<WorkOrder> matching)
    {
        await Click(nameof(UI.Shared.NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search**");
        var assigneeSelect = Page.Locator($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        await Expect(assigneeSelect).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await assigneeSelect.SelectOptionAsync(assignee.UserName);

        var culture = CultureInfo.GetCultureInfo("en-US");
        foreach (var wo in matching)
        {
            var cell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + wo.Number);
            await Expect(cell).ToBeAttachedAsync(new LocatorAssertionsToBeAttachedOptions { Timeout = 30_000 });
            await Expect(cell).ToContainTextAsync(wo.DueDate!.Value.ToString("d", culture));
        }
    }

    private static IReadOnlyList<DateOnly> ComingSaturdays()
    {
        var first = ChurchTimeZone.ComingSaturday(TimeProvider.System);
        return Enumerable.Range(0, 10).Select(i => first.AddDays(7 * i)).ToList();
    }
}
