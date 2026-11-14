using System.Collections.Concurrent;
using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

/// <summary>
/// MCP acceptance base: per-test MCP client (keyed by NUnit test id) and API seeding under
/// <see cref="ParallelScope.All"/>.
/// </summary>
public abstract class McpAcceptanceTestBase : AcceptanceApiTestBase
{
    private static readonly ConcurrentDictionary<string, McpTestHelper> Helpers = new();

    /// <summary>Connected MCP helper for this test.</summary>
    protected McpTestHelper Helper =>
        Helpers.TryGetValue(TestContext.CurrentContext.Test.ID, out var helper)
            ? helper
            : throw new InvalidOperationException("MCP helper is not initialized.");

    [SetUp]
    public async Task ConnectMcpAsync()
    {
        var helper = new McpTestHelper(TestHost.GetRequiredService<ChatClientFactory>());
        await helper.ConnectAsync();
        if (!helper.Connected)
        {
            await helper.DisposeAsync();
            Assert.Inconclusive("MCP server is not available");
        }

        Helpers[TestContext.CurrentContext.Test.ID] = helper;
    }

    [TearDown]
    public async Task DisposeMcpAsync()
    {
        if (!Helpers.TryRemove(TestContext.CurrentContext.Test.ID, out var helper))
        {
            return;
        }

        await helper.DisposeAsync();
    }

    /// <summary>Navigates to manage edit for <paramref name="workOrderNumber"/>.</summary>
    protected async Task NavigateToManageEditAsync(string workOrderNumber)
    {
        await Page.GotoAsync($"/workorder/manage/{workOrderNumber}?mode=Edit");
        var numberField = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(numberField).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await Expect(numberField).ToHaveTextAsync(workOrderNumber);
    }
}
