namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpWorkOrderLifecycleLlmTests : McpAcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [SetUp]
    public async Task EnsureChatClientAsync()
    {
        await SkipIfNoChatClient();
    }

    [Test, LlmTest]
    public async Task ShouldCompleteFullLifecycleViaLlm()
    {
        var creator = await SeedEmployeeAsync();
        var assignee = await SeedEmployeeAsync();
        var workOrderNumber = await CreateAssignedForLlmAsync(creator, assignee);

        var response = await Helper.SendPrompt(BuildBeginAndCompletePrompt(workOrderNumber, assignee.UserName));
        response.Text.ShouldNotBeNullOrEmpty();
        var responseText = response.Text.ToLowerInvariant();
        (responseText.Contains("complete") || responseText.Contains("completed"))
            .ShouldBeTrue($"Expected 'complete' status in response: {response.Text}");
    }

    private async Task<string> CreateAssignedForLlmAsync(Employee creator, Employee assignee)
    {
        var createResult = await Helper.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = $"[{TestTag}] LLM lifecycle test",
                ["description"] = "Testing full lifecycle via LLM",
                ["creatorUsername"] = creator.UserName
            });
        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        await Helper.CallToolDirectly("execute-work-order-command",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["commandName"] = "DraftToAssignedCommand",
                ["executingUsername"] = creator.UserName,
                ["assigneeUsername"] = assignee.UserName
            });
        return workOrderNumber;
    }

    private static string BuildBeginAndCompletePrompt(string workOrderNumber, string assigneeUserName) =>
        $"Work order '{workOrderNumber}' is currently in Assigned status, assigned to '{assigneeUserName}'.\n" +
        "Do these two steps using the execute-work-order-command tool:\n" +
        $"1. Call execute-work-order-command with workOrderNumber='{workOrderNumber}', commandName='AssignedToInProgressCommand', executingUsername='{assigneeUserName}'\n" +
        $"2. Call execute-work-order-command with workOrderNumber='{workOrderNumber}', commandName='InProgressToCompleteCommand', executingUsername='{assigneeUserName}'\n" +
        "Report the final status of the work order.";

    [Test, LlmTest]
    public async Task ShouldCreateAndAssignWorkOrderViaLlm()
    {
        var creator = await SeedEmployeeAsync();
        var assignee = await SeedEmployeeAsync();
        var title = $"[{TestTag}] Fix sanctuary lighting";

        var response = await Helper.SendPrompt(
            "Do these two steps:\n" +
            $"1. Call create-work-order with title='{title}', description='Replace burned out bulbs in the sanctuary', creatorUsername='{creator.UserName}'.\n" +
            "2. Take the work order Number from step 1 and call execute-work-order-command with that workOrderNumber, " +
            $"commandName='DraftToAssignedCommand', executingUsername='{creator.UserName}', assigneeUsername='{assignee.UserName}'.\n" +
            "Report the final status of the work order.");

        response.Text.ShouldNotBeNullOrEmpty();
        var responseText = response.Text.ToLowerInvariant();
        (responseText.Contains("assigned") || responseText.Contains("assign"))
            .ShouldBeTrue($"Expected 'assigned' status in response: {response.Text}");
    }
}
