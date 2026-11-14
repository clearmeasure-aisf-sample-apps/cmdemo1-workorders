using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

/// <summary>
/// Acceptance test for the AI Agent page (/ai-agent).
/// Pool slots use the stub model; do not point slots at the real model.
/// </summary>
[TestFixture]
public class ApplicationChatAgentTests : AiAgentsAcceptanceTestBase
{
    [SetUp]
    public async Task EnsureLlmAvailable()
    {
        await SkipUnlessAiAgentChatReadyAsync();
    }

    [Test, Ignore("Flaky on CI: MCP loopback ToolProvider produces JSON deserialization error on GitHub Actions runners")]
    public async Task ShouldCreateWorkOrderViaAiAgentChat()
    {
        var assignee = await SeedEmployeeAsync();
        await LoginAsCurrentUser();
        await OpenAiAgentPageAsync();

        var before = await SnapshotAssigneeOrderIdsAsync(assignee);
        var prompt =
            $"I am {CurrentUser.UserName}. " +
            $"Create a work order for {assignee.UserName} to mow the grass. " +
            "He should take care to edge around the prayer garden. " +
            $"Use '{CurrentUser.UserName}' as the creatorUsername. " +
            $"Put [{TestTag}] in the title. " +
            $"After creating it, assign it to {assignee.UserName} using the DraftToAssignedCommand " +
            $"with executingUsername='{CurrentUser.UserName}' and assigneeUsername='{assignee.UserName}'. " +
            "In your final response, include the work order number on its own line in exactly this format: " +
            "WorkOrderNumber: <number>";

        await SendChatPromptAsync(prompt);
        await AssertCreatedAssignedOrderAsync(assignee, before);
    }

    [Test]
    public async Task ShouldShowFriendlyMessageWhenProviderRefuses()
    {
        await RequirePoolSlotHostAsync();
        await LoginAsCurrentUser();
        await OpenAiAgentPageAsync();
        await SendChatPromptAsync($"Test prompt that {StubChatClient.RefusalPromptMarker}", 60_000);

        var messageText = await Page.GetByTestId(nameof(ApplicationChat.Elements.AiMessage) + "1").InnerTextAsync();
        messageText.ShouldContain("couldn't process that request");
        await Expect(Page.Locator(".error, [role='alert']")).ToHaveCountAsync(0);
    }

    private async Task AssertCreatedAssignedOrderAsync(Employee assignee, HashSet<Guid> before)
    {
        var chatText = await Page.GetByTestId(nameof(ApplicationChat.Elements.ChatHistory)).InnerTextAsync();
        chatText.ShouldNotBeNullOrEmpty();

        var match = Regex.Match(chatText, @"WorkOrderNumber:\s*[`*]*([A-Za-z0-9\-]+)[`*]*");
        match.Success.ShouldBeTrue($"Expected WorkOrderNumber in response: {chatText}");
        var workOrderNumber = match.Groups[1].Value.Trim('`', '*', ' ');

        var createdWo = await Bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
        createdWo ??= (await LoadTaggedAssigneeOrdersAsync(assignee))
            .FirstOrDefault(wo => !before.Contains(wo.Id));

        createdWo.ShouldNotBeNull($"Expected work order '{workOrderNumber}'");
        createdWo.Creator!.UserName.ShouldBe(CurrentUser.UserName);
        createdWo.Assignee!.UserName.ShouldBe(assignee.UserName);
        createdWo.Status.ShouldBe(WorkOrderStatus.Assigned);
        createdWo.Description.ShouldNotBeNullOrEmpty();

        var description = createdWo.Description!.ToLowerInvariant();
        (description.Contains("edge") || description.Contains("edging") || description.Contains("prayer garden"))
            .ShouldBeTrue($"Expected edging/prayer garden: {createdWo.Description}");
    }
}
