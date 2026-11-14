using System.Globalization;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpSaveWorkOrderAcceptanceTests : McpAcceptanceTestBase
{
    private const string DueTodayBackground = "rgb(254, 240, 138)";

    [Test]
    public async Task ShouldShowSavedTitleAndClearedDueDateOnManageAndSearch()
    {
        await LoginAsCurrentUser();
        const string instructions = "Watch for preschool play area";
        const string room = "Front lawn";
        var workOrderNumber = await CreateDraftForSavedTitleScenarioAsync(instructions, room);
        await SaveClearedDueDateTitleAsync(workOrderNumber);
        await NavigateToManageEditAsync(workOrderNumber);
        await ExpectSavedClearedFieldsAsync(instructions, room);
        await AssertSearchRowShowsSavedTitleAsync(workOrderNumber);
    }

    private Task<string> CreateDraftForSavedTitleScenarioAsync(string instructions, string room) =>
        CreateDraftAsync(
            $"[{TestTag}] mow front grass",
            "Weekly mowing task",
            new McpDraftCreateSpec
            {
                Instructions = instructions,
                RoomNumber = room,
                DueDate = "2027-06-15"
            });

    private async Task SaveClearedDueDateTitleAsync(string workOrderNumber)
    {
        var saveResult = await Helper.CallToolDirectly("save-work-order",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["executingUsername"] = CurrentUser.UserName,
                ["title"] = "Saturday mow",
                ["dueDate"] = string.Empty
            });
        saveResult.ShouldContain("Saturday mow");
    }

    [Test]
    public async Task ShouldShowChicagoTodayDueDateWithYellowOnManageAndSearch()
    {
        await LoginAsCurrentUser();
        var workOrderNumber = await CreateDraftAsync($"[{TestTag}] due today via save", "Due date coloring scenario");

        var today = ChurchTimeZone.Today(TimeProvider.System);
        var todayText = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await Helper.CallToolDirectly("save-work-order",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["executingUsername"] = CurrentUser.UserName,
                ["dueDate"] = todayText
            });

        await NavigateToManageEditAsync(workOrderNumber);
        var dueDateInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.DueDate));
        await Expect(dueDateInput).ToHaveValueAsync(todayText);
        (await BackgroundColorAsync(dueDateInput)).ShouldBe(DueTodayBackground);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status))).ToHaveTextAsync("Draft");

        await Page.EvaluateAsync("u => Blazor.navigateTo(u)", $"/workorder/search?Q={workOrderNumber}");
        var dueDateCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + workOrderNumber);
        await Expect(dueDateCell).ToBeAttachedAsync(new LocatorAssertionsToBeAttachedOptions { Timeout = 30_000 });
        await Expect(dueDateCell).ToHaveClassAsync(new Regex("due-date-today"));
        (await BackgroundColorAsync(dueDateCell)).ShouldBe(DueTodayBackground);
    }

    [Test]
    public async Task ShouldTruncate4001CharacterInstructionsOnManageEditWithoutValidationBanner()
    {
        await LoginAsCurrentUser();
        var workOrderNumber = await CreateDraftAsync($"[{TestTag}] save truncate instructions", "Truncation via save-work-order");

        var longInstructions = new string('S', WorkOrder.InstructionsMaxLength + 1);
        var expectedInstructions = new string('S', WorkOrder.InstructionsMaxLength);
        await Helper.CallToolDirectly("save-work-order",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["executingUsername"] = CurrentUser.UserName,
                ["instructions"] = longInstructions
            });

        await NavigateToManageEditAsync(workOrderNumber);
        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(expectedInstructions);
        await Expect(Page.GetByText("Instructions cannot exceed 4000 characters.")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task ShouldFailSaveForAssignedWorkOrderWithoutChangingManageFields()
    {
        await LoginAsCurrentUser();
        var assignee = await SeedEmployeeAsync();
        const string originalTitle = "Assigned save guard";
        var workOrderNumber = await CreateDraftAsync($"[{TestTag}] {originalTitle}", "Assigned scenario");
        await AssignDraftAsync(workOrderNumber, assignee.UserName);
        await AssertSaveRejectedAsync(workOrderNumber);
        await NavigateToManageEditAsync(workOrderNumber);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)))
            .ToHaveValueAsync($"[{TestTag}] {originalTitle}");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status))).ToHaveTextAsync("Assigned");
    }

    private async Task AssignDraftAsync(string workOrderNumber, string assigneeUserName)
    {
        await Helper.CallToolDirectly("execute-work-order-command",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["commandName"] = "DraftToAssignedCommand",
                ["executingUsername"] = CurrentUser.UserName,
                ["assigneeUsername"] = assigneeUserName
            });
    }

    private async Task AssertSaveRejectedAsync(string workOrderNumber)
    {
        var saveResult = await Helper.CallToolDirectly("save-work-order",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["executingUsername"] = CurrentUser.UserName,
                ["title"] = "Should not persist"
            });
        saveResult.ShouldContain("cannot be executed");
    }

    [Test]
    public async Task ShouldFailSaveForNonCreatorWithoutChangingManageFields()
    {
        await LoginAsCurrentUser();
        var otherUser = await SeedEmployeeAsync();
        const string originalTitle = "Non-creator save guard";
        var workOrderNumber = await CreateDraftAsync($"[{TestTag}] {originalTitle}", "Non-creator scenario");

        var saveResult = await Helper.CallToolDirectly("save-work-order",
            new Dictionary<string, object?>
            {
                ["workOrderNumber"] = workOrderNumber,
                ["executingUsername"] = otherUser.UserName,
                ["title"] = "Should not persist"
            });
        saveResult.ShouldContain("cannot be executed");

        await NavigateToManageEditAsync(workOrderNumber);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)))
            .ToHaveValueAsync($"[{TestTag}] {originalTitle}");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status))).ToHaveTextAsync("Draft");
    }

    private async Task ExpectSavedClearedFieldsAsync(string instructions, string room)
    {
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title))).ToHaveValueAsync("Saturday mow");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.DueDate))).ToHaveValueAsync(string.Empty);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions))).ToHaveValueAsync(instructions);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber))).ToHaveValueAsync(room);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status))).ToHaveTextAsync("Draft");
    }

    private async Task AssertSearchRowShowsSavedTitleAsync(string workOrderNumber)
    {
        await Page.EvaluateAsync("u => Blazor.navigateTo(u)", $"/workorder/search?Q={workOrderNumber}");
        var link = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + workOrderNumber);
        await Expect(link).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var row = SearchRowForWorkOrder(workOrderNumber);
        await Expect(row).ToContainTextAsync("Saturday mow");

        var dueDateCell = Page.GetByTestId(nameof(WorkOrderSearch.Elements.DueDateCell) + workOrderNumber);
        await Expect(dueDateCell).ToBeAttachedAsync();
        await Expect(dueDateCell).ToHaveTextAsync("–");
        await Expect(dueDateCell).Not.ToHaveClassAsync(new Regex("due-date-today|due-date-overdue"));
    }

    private async Task<string> CreateDraftAsync(
        string title,
        string description,
        McpDraftCreateSpec? spec = null)
    {
        var createResult = await Helper.CallToolDirectly(
            "create-work-order",
            BuildCreateArguments(title, description, spec ?? new McpDraftCreateSpec()));
        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();
        return workOrderNumber;
    }

    private Dictionary<string, object?> BuildCreateArguments(
        string title,
        string description,
        McpDraftCreateSpec spec)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["title"] = title,
            ["description"] = description,
            ["creatorUsername"] = CurrentUser.UserName
        };
        if (spec.Instructions is not null)
        {
            arguments["instructions"] = spec.Instructions;
        }

        if (spec.RoomNumber is not null)
        {
            arguments["roomNumber"] = spec.RoomNumber;
        }

        if (spec.DueDate is not null)
        {
            arguments["dueDate"] = spec.DueDate;
        }

        return arguments;
    }

    private ILocator SearchRowForWorkOrder(string workOrderNumber)
    {
        var link = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + workOrderNumber);
        return Page.Locator("tr").Filter(new LocatorFilterOptions { Has = link });
    }

    private static async Task<string> BackgroundColorAsync(ILocator locator) =>
        await locator.EvaluateAsync<string>("element => window.getComputedStyle(element).backgroundColor");
}
