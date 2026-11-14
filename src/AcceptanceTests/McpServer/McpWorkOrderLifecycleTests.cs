using System.Text.Json;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpWorkOrderLifecycleTests : McpAcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task ShouldCompleteFullLifecycleViaDirectToolCalls()
    {
        var creator = await SeedEmployeeAsync();
        var assignee = await SeedEmployeeAsync();

        var createResult = await Helper.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = $"[{TestTag}] Lifecycle test work order",
                ["description"] = "Testing full lifecycle via direct MCP tool calls",
                ["creatorUsername"] = creator.UserName
            });

        createResult.ShouldContain($"[{TestTag}] Lifecycle test work order");
        createResult.ShouldContain("Draft");
        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrEmpty("Work order number should be returned");

        await AssignBeginAndCompleteAsync(workOrderNumber, creator.UserName, assignee.UserName);
        await AssertCompletedDetailAsync(workOrderNumber, creator, assignee);
    }

    private async Task AssignBeginAndCompleteAsync(
        string workOrderNumber,
        string creatorUserName,
        string assigneeUserName)
    {
        await ExecuteLifecycleCommandAsync(workOrderNumber, new LifecycleCommandSpec
        {
            CommandName = "DraftToAssignedCommand",
            ExecutingUserName = creatorUserName,
            AssigneeUserName = assigneeUserName,
            ExpectedStatusFragment = "Assigned"
        });
        await ExecuteLifecycleCommandAsync(workOrderNumber, new LifecycleCommandSpec
        {
            CommandName = "AssignedToInProgressCommand",
            ExecutingUserName = assigneeUserName,
            ExpectedStatusFragment = "In Progress"
        });
        var completeResult = await ExecuteLifecycleCommandAsync(workOrderNumber, new LifecycleCommandSpec
        {
            CommandName = "InProgressToCompleteCommand",
            ExecutingUserName = assigneeUserName,
            ExpectedStatusFragment = "Complete"
        });
        completeResult.ShouldContain("CompletedDate");
    }

    private async Task<string> ExecuteLifecycleCommandAsync(string workOrderNumber, LifecycleCommandSpec spec)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["workOrderNumber"] = workOrderNumber,
            ["commandName"] = spec.CommandName,
            ["executingUsername"] = spec.ExecutingUserName
        };
        if (spec.AssigneeUserName is not null)
        {
            arguments["assigneeUsername"] = spec.AssigneeUserName;
        }

        var result = await Helper.CallToolDirectly("execute-work-order-command", arguments);
        result.ShouldContain(spec.ExpectedStatusFragment);
        return result;
    }

    private sealed class LifecycleCommandSpec
    {
        public required string CommandName { get; init; }
        public required string ExecutingUserName { get; init; }
        public string? AssigneeUserName { get; init; }
        public required string ExpectedStatusFragment { get; init; }
    }

    private async Task AssertCompletedDetailAsync(
        string workOrderNumber,
        Employee creator,
        Employee assignee)
    {
        var getResult = await Helper.CallToolDirectly("get-work-order",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber
            });

        getResult.ShouldContain("Complete");
        getResult.ShouldContain($"[{TestTag}] Lifecycle test work order");
        using var getDocument = JsonDocument.Parse(getResult);
        var detail = getDocument.RootElement;
        detail.GetProperty("Creator").GetString().ShouldBe(creator.GetFullName());
        detail.GetProperty("CreatorUsername").GetString().ShouldBe(creator.UserName);
        detail.GetProperty("Assignee").GetString().ShouldBe(assignee.GetFullName());
        detail.GetProperty("AssigneeUsername").GetString().ShouldBe(assignee.UserName);
    }
}
