using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Payloads;

[TestFixture]
public class WorkOrderStatusJsonTests
{
    [Test]
    public void Serialize_Should_WriteCodeKeyAndFriendlyName()
    {
        WorkOrderStatusJson[] statuses = [new("DFT", "Draft", "Draft")];

        var json = JsonSerializer.Serialize(statuses, McpJsonContext.Default.WorkOrderStatusJsonArray);

        JsonDocument.Parse(json).RootElement[0].GetProperty("FriendlyName").GetString().ShouldBe("Draft");
    }
}
