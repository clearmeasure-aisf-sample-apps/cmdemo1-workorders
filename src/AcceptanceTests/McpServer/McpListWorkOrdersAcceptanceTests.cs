using System.Text.Json;
using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

/// <summary>
/// MCP list-work-orders filters match portal search for this test's own TestTag-scoped rows.
/// </summary>
[TestFixture]
public class McpListWorkOrdersAcceptanceTests : McpAcceptanceTestBase
{
    [Test]
    public async Task ShouldMatchMcpUsernameFiltersToPortalSearch()
    {
        var seed = await SeedTaggedListScenarioAsync();
        await LoginAsCurrentUser();
        await OpenTaggedWorkOrderSearchAsync();

        await AssertCreatorFilterContainsAsync(seed.CreatorDraft, seed.CreatorUserName);
        await AssertAssigneeFilterContainsAsync(seed);
        await AssertAssigneeInProgressFilterAsync(seed);
        await AssertCreatorDraftFilterAsync(seed);
        await AssertSeededOrdersVisibleWhenUnfilteredAsync(seed);
        await AssertUnknownCreatorLeavesPortalUnchangedAsync();
        await AssertManageShowsSeededCreatorAsync(seed);
    }

    private async Task<McpListWorkOrderSeed> SeedTaggedListScenarioAsync()
    {
        var creator = await SeedListActorAsync("c");
        var assignee = await SeedListActorAsync("a");
        var otherCreator = await SeedEmployeeAsync();
        var otherAssignee = await SeedEmployeeAsync();
        var numbers = BuildSeedNumbers(creator, assignee);
        await SeedOwnedOrdersAsync(numbers, creator, assignee, otherAssignee);
        await SeedPeerOrdersAsync(otherCreator, assignee, otherAssignee);
        return numbers;
    }

    private Task<Employee> SeedListActorAsync(string suffix) =>
        SeedEmployeeAsync(new TestSupportEmployeeFields
        {
            FirstName = "List",
            LastName = $"!{TestTag}{suffix}"
        });

    private McpListWorkOrderSeed BuildSeedNumbers(Employee creator, Employee assignee) =>
        new()
        {
            CreatorUserName = creator.UserName,
            AssigneeUserName = assignee.UserName,
            CreatorFullName = creator.GetFullName(),
            CreatorDraft = Number("LJD"),
            AssigneeAssigned = Number("WA"),
            AssigneeInProgress = Number("WI"),
            CreatorComplete = Number("LJC")
        };

    private async Task SeedOwnedOrdersAsync(
        McpListWorkOrderSeed numbers,
        Employee creator,
        Employee assignee,
        Employee otherAssignee)
    {
        await SeedRoleAsync(creator, otherAssignee, numbers.CreatorDraft, WorkOrderStatus.Draft);
        await SeedRoleAsync(creator, assignee, numbers.AssigneeAssigned, WorkOrderStatus.Assigned);
        await SeedRoleAsync(creator, assignee, numbers.AssigneeInProgress, WorkOrderStatus.InProgress);
        await SeedRoleAsync(creator, otherAssignee, numbers.CreatorComplete, WorkOrderStatus.Complete);
    }

    private async Task SeedPeerOrdersAsync(
        Employee otherCreator,
        Employee assignee,
        Employee otherAssignee)
    {
        await SeedRoleAsync(otherCreator, otherAssignee, Number("OD"), WorkOrderStatus.Draft);
        await SeedRoleAsync(otherCreator, assignee, Number("OWI"), WorkOrderStatus.InProgress);
    }

    private Task SeedRoleAsync(
        Employee creator,
        Employee assignee,
        string number,
        WorkOrderStatus status) =>
        SeedWorkOrderAsync(creator, status, new WorkOrderSeedFields
        {
            Number = number,
            Title = $"[{TestTag}] MCP list {number}",
            Description = "MCP list independence acceptance test",
            AssigneeUserName = assignee.UserName
        });

    private string Number(string role)
    {
        var number = $"{TestTag[..4]}{role}";
        number.Length.ShouldBeLessThanOrEqualTo(7);
        return number;
    }

    private async Task AssertCreatorFilterContainsAsync(string requiredNumber, string creatorUserName)
    {
        var mcpNumbers = await CallListWorkOrders(
            new Dictionary<string, object?> { ["creatorUsername"] = creatorUserName });
        mcpNumbers.ShouldContain(requiredNumber);
        await AssertPortalAndMcpMatchAsync([requiredNumber], creatorUsername: creatorUserName);
    }

    private async Task AssertAssigneeFilterContainsAsync(McpListWorkOrderSeed seed)
    {
        var mcpNumbers = await CallListWorkOrders(
            new Dictionary<string, object?> { ["assigneeUsername"] = seed.AssigneeUserName });
        mcpNumbers.ShouldContain(seed.AssigneeAssigned);
        mcpNumbers.ShouldContain(seed.AssigneeInProgress);
        await AssertPortalAndMcpMatchAsync(
            [seed.AssigneeAssigned, seed.AssigneeInProgress],
            assigneeUsername: seed.AssigneeUserName);
    }

    private async Task AssertAssigneeInProgressFilterAsync(McpListWorkOrderSeed seed)
    {
        var mcpNumbers = await CallListWorkOrders(
            new Dictionary<string, object?>
            {
                ["status"] = "InProgress",
                ["assigneeUsername"] = seed.AssigneeUserName
            });
        mcpNumbers.ShouldContain(seed.AssigneeInProgress);
        mcpNumbers.ShouldNotContain(seed.AssigneeAssigned);
        await AssertPortalAndMcpMatchAsync(
            [seed.AssigneeInProgress],
            assigneeUsername: seed.AssigneeUserName,
            status: "InProgress");
    }

    private async Task AssertCreatorDraftFilterAsync(McpListWorkOrderSeed seed)
    {
        var mcpNumbers = await CallListWorkOrders(
            new Dictionary<string, object?>
            {
                ["status"] = "Draft",
                ["creatorUsername"] = seed.CreatorUserName
            });
        mcpNumbers.ShouldContain(seed.CreatorDraft);
        mcpNumbers.ShouldNotContain(seed.AssigneeAssigned);
        mcpNumbers.ShouldNotContain(seed.CreatorComplete);
        await AssertPortalAndMcpMatchAsync(
            [seed.CreatorDraft],
            creatorUsername: seed.CreatorUserName,
            status: "Draft");
    }

    private async Task AssertSeededOrdersVisibleWhenUnfilteredAsync(McpListWorkOrderSeed seed)
    {
        var mcpNumbers = await CallListWorkOrders([]);
        mcpNumbers.ShouldContain(seed.CreatorDraft);
        mcpNumbers.ShouldContain(seed.AssigneeAssigned);
        mcpNumbers.ShouldContain(seed.AssigneeInProgress);
        mcpNumbers.ShouldContain(seed.CreatorComplete);

        await OpenTaggedWorkOrderSearchAsync();
        var portalNumbers = await VisibleSearchNumbers();
        portalNumbers.ShouldContain(seed.CreatorDraft);
        portalNumbers.ShouldContain(seed.AssigneeAssigned);
        portalNumbers.ShouldContain(seed.AssigneeInProgress);
        portalNumbers.ShouldContain(seed.CreatorComplete);
    }

    private async Task OpenTaggedWorkOrderSearchAsync()
    {
        await Page.EvaluateAsync("u => Blazor.navigateTo(u)", $"/workorder/search?Q={TestTag}");
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}")).ToBeVisibleAsync();
        await Expect(Page.Locator(".grid-data tbody")).ToBeVisibleAsync();
    }

    private async Task AssertUnknownCreatorLeavesPortalUnchangedAsync()
    {
        var searchNumbersBeforeUnknown = await VisibleSearchNumbers();
        var unknownNumbers = await CallListWorkOrders(
            new Dictionary<string, object?> { ["creatorUsername"] = "not-a-person" });
        unknownNumbers.ShouldBeEmpty();
        (await VisibleSearchNumbers()).ShouldBeSet(searchNumbersBeforeUnknown);
    }

    private async Task AssertManageShowsSeededCreatorAsync(McpListWorkOrderSeed seed)
    {
        await Page.EvaluateAsync("u => Blazor.navigateTo(u)", $"/workorder/search?Q={seed.CreatorDraft}");
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + seed.CreatorDraft);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber)))
            .ToHaveTextAsync(seed.CreatorDraft);
        var creator = Page.Locator(".form-group")
            .Filter(new LocatorFilterOptions { HasText = "Creator" })
            .Locator(".value");
        await Expect(creator).ToHaveTextAsync(seed.CreatorFullName);
    }

    private async Task AssertPortalAndMcpMatchAsync(
        IReadOnlyCollection<string> requiredNumbers,
        string? creatorUsername = null,
        string? assigneeUsername = null,
        string? status = null)
    {
        var filterArguments = BuildListWorkOrdersArguments(creatorUsername, assigneeUsername, status);
        await OpenFilteredTaggedSearchAsync(creatorUsername, assigneeUsername, status);
        await WaitForRequiredPortalRowsAsync(requiredNumbers);

        var portalNumbers = await VisibleSearchNumbers();
        var mcpNumbers = await CallListWorkOrders(filterArguments);
        requiredNumbers.All(portalNumbers.Contains).ShouldBeTrue(
            $"Portal missing required numbers. Required={FormatSet(requiredNumbers)}; Portal={FormatSet(portalNumbers)}");
        requiredNumbers.All(mcpNumbers.Contains).ShouldBeTrue(
            $"MCP missing required numbers. Required={FormatSet(requiredNumbers)}; MCP={FormatSet(mcpNumbers)}");
        portalNumbers.ShouldBeSet(mcpNumbers);
    }

    private async Task OpenFilteredTaggedSearchAsync(
        string? creatorUsername,
        string? assigneeUsername,
        string? status)
    {
        var query = new List<string> { $"Q={Uri.EscapeDataString(TestTag)}" };
        if (!string.IsNullOrEmpty(creatorUsername))
        {
            query.Add($"Creator={Uri.EscapeDataString(creatorUsername)}");
        }

        if (!string.IsNullOrEmpty(assigneeUsername))
        {
            query.Add($"Assignee={Uri.EscapeDataString(assigneeUsername)}");
        }

        if (!string.IsNullOrEmpty(status))
        {
            query.Add($"Status={Uri.EscapeDataString(status)}");
        }

        await Page.EvaluateAsync("u => Blazor.navigateTo(u)", $"/workorder/search?{string.Join("&", query)}");
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}")).ToBeVisibleAsync();
        if (!string.IsNullOrEmpty(creatorUsername))
        {
            await Expect(Page.Locator($"#{WorkOrderSearch.Elements.CreatorSelect}"))
                .ToHaveValueAsync(creatorUsername);
        }
    }

    private async Task WaitForRequiredPortalRowsAsync(IReadOnlyCollection<string> requiredNumbers)
    {
        foreach (var number in requiredNumbers)
        {
            await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + number))
                .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        }
    }

    private async Task<HashSet<string>> CallListWorkOrders(Dictionary<string, object?> arguments)
    {
        var json = await Helper.CallToolDirectly("list-work-orders", arguments);
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray()
            .Select(item => item.GetProperty("Number").GetString()!)
            .ToHashSet();
    }

    private static Dictionary<string, object?> BuildListWorkOrdersArguments(
        string? creatorUsername,
        string? assigneeUsername,
        string? status)
    {
        var arguments = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(creatorUsername))
        {
            arguments["creatorUsername"] = creatorUsername;
        }

        if (!string.IsNullOrEmpty(assigneeUsername))
        {
            arguments["assigneeUsername"] = assigneeUsername;
        }

        if (!string.IsNullOrEmpty(status))
        {
            arguments["status"] = status;
        }

        return arguments;
    }

    private static string FormatSet(IEnumerable<string> numbers) =>
        string.Join(", ", numbers.OrderBy(number => number));

    private async Task<HashSet<string>> VisibleSearchNumbers()
    {
        var numbers = new HashSet<string>();
        var next = Page.GetByTestId(nameof(WorkOrderSearch.Elements.PageNextButton));
        numbers.UnionWith(await NumbersOnCurrentPage());
        while (await next.IsEnabledAsync())
        {
            var firstLink = await FirstLinkTestId();
            await next.ClickAsync();
            await Expect(Page.Locator($"[data-testid^='{WorkOrderSearch.Elements.WorkOrderLink}']").First)
                .Not.ToHaveAttributeAsync("data-testid", firstLink!);
            numbers.UnionWith(await NumbersOnCurrentPage());
        }

        await ReturnToFirstSearchPageAsync();
        return numbers;
    }

    private async Task ReturnToFirstSearchPageAsync()
    {
        var firstPage = Page.GetByTestId($"{WorkOrderSearch.Elements.PageButton}1");
        if (await firstPage.GetAttributeAsync("aria-current") != "page")
        {
            await firstPage.ClickAsync();
            await Expect(firstPage).ToHaveAttributeAsync("aria-current", "page");
        }
    }

    private async Task<string?> FirstLinkTestId() =>
        await Page.Locator($"[data-testid^='{WorkOrderSearch.Elements.WorkOrderLink}']").First
            .GetAttributeAsync("data-testid");

    private async Task<IEnumerable<string>> NumbersOnCurrentPage()
    {
        var links = Page.Locator($"[data-testid^='{WorkOrderSearch.Elements.WorkOrderLink}']");
        var prefixLength = nameof(WorkOrderSearch.Elements.WorkOrderLink).Length;
        var numbers = new List<string>();
        for (var index = 0; index < await links.CountAsync(); index++)
        {
            var testId = await links.Nth(index).GetAttributeAsync("data-testid");
            numbers.Add(testId![prefixLength..]);
        }

        return numbers;
    }
}

internal static class McpListWorkOrderNumberAssertions
{
    public static void ShouldBeSet(this IEnumerable<string> actual, IEnumerable<string> expected)
    {
        actual.OrderBy(number => number).ShouldBe(expected.OrderBy(number => number));
    }
}
