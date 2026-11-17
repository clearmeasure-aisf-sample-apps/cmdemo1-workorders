using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Payloads;

[TestFixture]
public class StatusTransitionJsonTests
{
    [Test]
    public void Serialize_Should_WriteCommandAndTargetStatus()
    {
        var transitions = new StatusTransitionsJson([new StatusTransitionJson("Assign", "Assigned")], [], [], []);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(transitions, McpJsonContext.Default.StatusTransitionsJson));

        var assign = json.RootElement.GetProperty("Draft")[0];
        assign.GetProperty("Command").GetString().ShouldBe("Assign");
        assign.GetProperty("TargetStatus").GetString().ShouldBe("Assigned");
    }
}
