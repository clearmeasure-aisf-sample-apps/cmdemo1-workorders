using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Payloads;

/// <summary>The role shape MCP tools answer with, written through the source-generated <see cref="McpJsonContext"/>.</summary>
[TestFixture]
public class RoleJsonTests
{
    [Test]
    public void From_Should_CopyNameAndPermissions_InPascalCaseJson()
    {
        RoleJson[] roles = [RoleJson.From(new Role("Fulfiller", canCreate: false, canFulfill: true))];

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(roles, McpJsonContext.Default.RoleJsonArray));

        var role = json.RootElement[0];
        role.GetProperty("Name").GetString().ShouldBe("Fulfiller");
        role.GetProperty("CanCreateWorkOrder").GetBoolean().ShouldBeFalse();
        role.GetProperty("CanFulfillWorkOrder").GetBoolean().ShouldBeTrue();
    }
}
