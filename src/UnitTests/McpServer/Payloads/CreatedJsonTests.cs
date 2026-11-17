using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Payloads;

/// <summary>The create-result shape, the one MCP payload written camelCase (<see cref="McpCamelCaseJsonContext"/>).</summary>
[TestFixture]
public class CreatedJsonTests
{
    [Test]
    public void Serialize_Should_WriteSucceededMessageAndId_InCamelCase()
    {
        var created = new CreatedJson(true, "Team created.", Guid.Empty);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(created, McpCamelCaseJsonContext.Default.CreatedJson));

        json.RootElement.GetProperty("succeeded").GetBoolean().ShouldBeTrue();
        json.RootElement.GetProperty("message").GetString().ShouldBe("Team created.");
        json.RootElement.GetProperty("id").GetGuid().ShouldBe(Guid.Empty);
    }
}
