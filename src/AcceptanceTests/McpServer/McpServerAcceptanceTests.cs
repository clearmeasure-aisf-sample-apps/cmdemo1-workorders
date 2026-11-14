namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpServerAcceptanceTests : McpAcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public void ShouldDiscoverAllMcpTools()
    {
        Helper.Tools.Count.ShouldBeGreaterThanOrEqualTo(6);

        var toolNames = Helper.Tools.Select(t => t.Name).ToList();
        toolNames.ShouldContain("list-work-orders");
        toolNames.ShouldContain("get-work-order");
        toolNames.ShouldContain("create-work-order");
        toolNames.ShouldContain("execute-work-order-command");
        toolNames.ShouldContain("list-employees");
        toolNames.ShouldContain("get-employee");
    }

    [Test]
    public async Task ShouldCreateWorkOrderViaDirectToolCall()
    {
        var creator = await SeedEmployeeAsync();

        var result = await Helper.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = $"[{TestTag}] Direct MCP tool test",
                ["description"] = "Created via direct tool call",
                ["creatorUsername"] = creator.UserName
            });

        result.ShouldContain($"[{TestTag}] Direct MCP tool test");
        result.ShouldContain("Draft");
    }
}
