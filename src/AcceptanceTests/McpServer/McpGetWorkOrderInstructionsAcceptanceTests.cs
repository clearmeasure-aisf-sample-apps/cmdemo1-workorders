using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpGetWorkOrderInstructionsAcceptanceTests : McpAcceptanceTestBase
{
    [Test]
    public async Task ShouldMatchGetWorkOrderDetailWithManageWhenInstructionsSet()
    {
        await LoginAsCurrentUser();

        const string title = "Saturday mow";
        const string description = "Mow the front lawn";
        const string instructions = "preschool quiet";
        const string room = "Front lawn";
        const string dueDate = "2027-06-15";
        var details = new McpDraftCreateSpec
        {
            Instructions = instructions,
            RoomNumber = room,
            DueDate = dueDate
        };
        var workOrderNumber = await CreateDraftWithDetailsAsync(title, description, details);

        await AssertGetWorkOrderShapeAsync(workOrderNumber, title, description, details);
        await AssertManageMatchesLiveGetAsync(workOrderNumber);

        var unknownResult = await GetWorkOrderAsync("ZZ9103");
        unknownResult.ShouldBe("No work order found with number 'ZZ9103'.");
    }

    [Test]
    public async Task ShouldMatchEmptyManageInstructionsWithPresentEmptyGetProperty()
    {
        await LoginAsCurrentUser();

        var createResult = await Helper.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = $"[{TestTag}] No special instructions",
                ["description"] = "Instructions intentionally omitted",
                ["creatorUsername"] = CurrentUser.UserName
            });
        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();

        var getResult = await GetWorkOrderAsync(workOrderNumber);
        using var document = JsonDocument.Parse(getResult);
        var instructions = document.RootElement.GetProperty("Instructions");
        instructions.ValueKind.ShouldBe(JsonValueKind.String);
        instructions.GetString().ShouldBe(string.Empty);
        getResult.ShouldContain("\"Instructions\": \"\"");
        getResult.ShouldNotContain("\"Instructions\": \"null\"");

        await NavigateToManageEditAsync(workOrderNumber);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions)))
            .ToHaveValueAsync(string.Empty);
    }

    private async Task<string> CreateDraftWithDetailsAsync(
        string title,
        string description,
        McpDraftCreateSpec details)
    {
        var createResult = await Helper.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = title,
                ["description"] = description,
                ["creatorUsername"] = CurrentUser.UserName,
                ["roomNumber"] = details.RoomNumber,
                ["dueDate"] = details.DueDate,
                ["instructions"] = details.Instructions
            });
        var workOrderNumber = McpTestHelper.ExtractJsonValue(createResult, "Number");
        workOrderNumber.ShouldNotBeNullOrWhiteSpace();
        return workOrderNumber;
    }

    private async Task AssertGetWorkOrderShapeAsync(
        string workOrderNumber,
        string title,
        string description,
        McpDraftCreateSpec details)
    {
        var getResult = await GetWorkOrderAsync(workOrderNumber);
        using var document = JsonDocument.Parse(getResult);
        var detail = document.RootElement;

        RequiredString(detail, "Instructions").ShouldBe(details.Instructions);
        RequiredString(detail, "Title").ShouldBe(title);
        RequiredString(detail, "Description").ShouldBe(description);
        RequiredString(detail, "RoomNumber").ShouldBe(details.RoomNumber);
        RequiredString(detail, "DueDate").ShouldBe(details.DueDate);
        RequiredString(detail, "Status").ShouldBe("Draft");
        RequiredString(detail, "CreatorUsername").ShouldBe(CurrentUser.UserName);
        detail.GetProperty("Assignee").ValueKind.ShouldBe(JsonValueKind.Null);
        detail.GetProperty("AssigneeUsername").ValueKind.ShouldBe(JsonValueKind.Null);
        detail.GetProperty("AssignedDate").ValueKind.ShouldBe(JsonValueKind.Null);
        detail.GetProperty("CompletedDate").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    private async Task AssertManageMatchesLiveGetAsync(string workOrderNumber)
    {
        await NavigateToManageEditAsync(workOrderNumber);

        // AutoReformatAgent can add trailing punctuation after create. Re-read MCP
        // detail for the Manage match so UI and get-work-order stay aligned.
        var getResult = await GetWorkOrderAsync(workOrderNumber);
        using var liveDocument = JsonDocument.Parse(getResult);
        var live = liveDocument.RootElement;
        await AssertManageCoreFieldsAsync(live);
        await AssertManageCreatorAndDatesAsync(live);
    }

    private async Task AssertManageCoreFieldsAsync(JsonElement live)
    {
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)))
            .ToHaveValueAsync(RequiredString(live, "Title"));
        var expectedDescription = RequiredString(live, "Description").TrimEnd();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description)))
            .ToHaveValueAsync(new Regex("^" + Regex.Escape(expectedDescription.TrimEnd('.')) + @"\.?$"));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions)))
            .ToHaveValueAsync(RequiredString(live, "Instructions"));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber)))
            .ToHaveValueAsync(RequiredString(live, "RoomNumber"));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.DueDate)))
            .ToHaveValueAsync(RequiredString(live, "DueDate"));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(RequiredString(live, "Status"));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee)))
            .ToHaveValueAsync(string.Empty);
    }

    private async Task AssertManageCreatorAndDatesAsync(JsonElement live)
    {
        var creator = RequiredString(live, "Creator");
        var creatorGroup = Page.Locator(".form-group").Filter(new LocatorFilterOptions { HasText = "Creator" });
        await Expect(creatorGroup.Locator("span.value").GetByText(creator, new LocatorGetByTextOptions { Exact = true }))
            .ToBeVisibleAsync();

        var createdDate = live.GetProperty("CreatedDate").GetDateTime();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.CreatedDate)))
            .ToHaveTextAsync(createdDate.ToString("G", CultureInfo.GetCultureInfo("en-US")));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.AssignedDate)))
            .ToHaveTextAsync(string.Empty);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.CompletedDate)))
            .ToHaveTextAsync(string.Empty);
    }

    private static string RequiredString(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetString()
        ?? throw new InvalidOperationException($"Property '{propertyName}' was null.");

    private Task<string> GetWorkOrderAsync(string workOrderNumber) =>
        Helper.CallToolDirectly("get-work-order",
            new Dictionary<string, object?> { ["workOrderNumber"] = workOrderNumber });
}
