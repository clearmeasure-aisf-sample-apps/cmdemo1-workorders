using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class McpCreateTeamToolSchemaTests
{
    [Test]
    public void CreateTeam_Should_ExposeMemberUserNamesInInputSchema_When_RegisteredAsMcpTool()
    {
        // Register the tool exactly the way McpServerApplication does for the MCP server.
        var services = new ServiceCollection();
        services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new()
                {
                    Name = "ChurchBulletin",
                    Version = "1.0.0"
                };
            })
            .WithTools<TeamTools>();

        using var provider = services.BuildServiceProvider();
        var tool = provider.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "create-team");

        tool.ProtocolTool.InputSchema.GetProperty("properties").TryGetProperty("memberUserNames", out _).ShouldBeTrue();
    }
}
