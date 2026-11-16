using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Payloads;

[TestFixture]
public class WorkOrderDetailJsonTests
{
    [Test]
    public void Serialize_Should_WriteDatesAsIso8601_And_KeepInstructions()
    {
        var detail = new WorkOrderDetailJson
        {
            Number = "AB12CD3", Instructions = "Use the side door.", Status = "Assigned", Priority = "High",
            CreatedDate = new DateTime(2026, 11, 16, 15, 0, 0, DateTimeKind.Utc)
        };

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(detail, McpJsonContext.Default.WorkOrderDetailJson));

        json.RootElement.GetProperty("Instructions").GetString().ShouldBe("Use the side door.");
        json.RootElement.GetProperty("CreatedDate").GetString().ShouldBe("2026-11-16T15:00:00Z");
    }
}
