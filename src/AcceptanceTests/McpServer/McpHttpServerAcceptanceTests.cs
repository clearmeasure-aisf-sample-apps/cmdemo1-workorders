namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpHttpServerAcceptanceTests : McpAcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public void ShouldDiscoverAllMcpToolsViaHttp()
    {
        var tools = Helper.Tools;
        tools.Count.ShouldBeGreaterThanOrEqualTo(6);

        var toolNames = tools.Select(t => t.Name).ToList();
        toolNames.ShouldContain("list-work-orders");
        toolNames.ShouldContain("get-work-order");
        toolNames.ShouldContain("create-work-order");
        toolNames.ShouldContain("execute-work-order-command");
        toolNames.ShouldContain("list-employees");
        toolNames.ShouldContain("get-employee");
    }

    [Test]
    [Category("Smoke")]
    public async Task ShouldListWorkOrdersViaHttp()
    {
        var creator = await SeedEmployeeAsync();
        var seeded = await SeedWorkOrderAsync(creator);
        seeded.Number.ShouldNotBeNullOrWhiteSpace();
        var text = await Helper.CallToolDirectly("list-work-orders",
            new Dictionary<string, object?> { ["creatorUsername"] = creator.UserName });

        text.ShouldNotBeNullOrEmpty();
        text.ShouldContain(seeded.Number!);
    }

    [Test]
    public async Task ShouldCreateWorkOrderViaHttp()
    {
        var creator = await SeedEmployeeAsync();

        var text = await Helper.CallToolDirectly("create-work-order",
            new Dictionary<string, object?>
            {
                ["title"] = $"[{TestTag}] HTTP transport test",
                ["description"] = "Created via HTTP MCP transport",
                ["creatorUsername"] = creator.UserName
            });

        text.ShouldContain($"[{TestTag}] HTTP transport test");
        text.ShouldContain("Draft");
    }

    [Test]
    public async Task ShouldGetEmployeeViaHttp()
    {
        var known = await SeedEmployeeAsync();

        var text = await Helper.CallToolDirectly("get-employee",
            new Dictionary<string, object?>
            {
                ["username"] = known.UserName
            });

        text.ShouldNotBeNullOrEmpty();
        text.ShouldContain(known.UserName);
    }
}
