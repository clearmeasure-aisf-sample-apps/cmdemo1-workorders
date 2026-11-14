using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpCreateWorkOrderInstructionsAcceptanceTests : McpAcceptanceTestBase
{
    [Test]
    public async Task ShouldShowExplicitInstructionsOnManageEditAfterMcpCreate()
    {
        await LoginAsCurrentUser();

        const string description = "Repair the fellowship hall window latch.";
        var instructions = $"[{TestTag}] Enter through north door; ladder in east shed";
        var workOrderNumber = await CreateDraftAsync(
            $"[{TestTag}] MCP instructions explicit",
            description,
            instructions);

        await NavigateToManageEditAsync(workOrderNumber);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(instructions);

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync(description);
        await Expect(instructionsField).Not.ToHaveValueAsync(description);
    }

    [Test]
    public async Task ShouldSaveSuccessfullyWhenInstructionsOmittedAfterMcpCreate()
    {
        await LoginAsCurrentUser();
        var workOrderNumber = await CreateDraftAsync(
            $"[{TestTag}] MCP instructions omitted",
            "Omitted instructions scenario");

        await NavigateToManageEditAsync(workOrderNumber);
        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(string.Empty);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        await NavigateToManageEditAsync(workOrderNumber);
        await Expect(instructionsField).ToHaveValueAsync(string.Empty);
    }

    [Test]
    public async Task ShouldSaveSuccessfullyWhenInstructionsEmptyAfterMcpCreate()
    {
        await LoginAsCurrentUser();
        var workOrderNumber = await CreateDraftAsync(
            $"[{TestTag}] MCP instructions empty",
            "Empty instructions scenario",
            string.Empty);

        await NavigateToManageEditAsync(workOrderNumber);
        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(string.Empty);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        await NavigateToManageEditAsync(workOrderNumber);
        await Expect(instructionsField).ToHaveValueAsync(string.Empty);
    }

    [Test]
    public async Task ShouldTruncate4001CharacterInstructionsOnManageEditWithoutValidationBanner()
    {
        await LoginAsCurrentUser();

        var longInstructions = new string('M', WorkOrder.InstructionsMaxLength + 1);
        var expectedInstructions = new string('M', WorkOrder.InstructionsMaxLength);
        var workOrderNumber = await CreateDraftAsync(
            $"[{TestTag}] MCP instructions truncate",
            "Truncation scenario",
            longInstructions);

        await NavigateToManageEditAsync(workOrderNumber);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(expectedInstructions);
        await Expect(Page.GetByText("Instructions cannot exceed 4000 characters.")).Not.ToBeVisibleAsync();
    }

    private async Task<string> CreateDraftAsync(string title, string description, string? instructions = null)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["title"] = title,
            ["description"] = description,
            ["creatorUsername"] = CurrentUser.UserName
        };
        if (instructions is not null)
        {
            arguments["instructions"] = instructions;
        }

        var createResult = await Helper.CallToolDirectly("create-work-order", arguments);
        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();
        return workOrderNumber;
    }
}
