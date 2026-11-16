using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Payloads;

[TestFixture]
public class StatusTransitionsJsonTests
{
    [Test]
    public void Serialize_Should_WriteOneArrayPerStatus_WithCompleteEmpty()
    {
        var transitions = new StatusTransitionsJson([], [], [], []);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(transitions, McpJsonContext.Default.StatusTransitionsJson));

        json.RootElement.EnumerateObject().Select(property => property.Name).ShouldBe(["Draft", "Assigned", "InProgress", "Complete"]);
        json.RootElement.GetProperty("Complete").GetArrayLength().ShouldBe(0);
    }
}
