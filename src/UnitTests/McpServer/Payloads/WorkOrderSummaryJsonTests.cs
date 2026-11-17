using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Payloads;

[TestFixture]
public class WorkOrderSummaryJsonTests
{
    [Test]
    public void Serialize_Should_WriteStatusAndPriority_AndNullForMissingOptionalFields()
    {
        WorkOrderSummaryJson[] summaries = [new() { Number = "AB12CD3", Status = "Draft", Priority = "Normal" }];

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(summaries, McpJsonContext.Default.WorkOrderSummaryJsonArray));

        var summary = json.RootElement[0];
        summary.GetProperty("Number").GetString().ShouldBe("AB12CD3");
        summary.GetProperty("Status").GetString().ShouldBe("Draft");
        summary.GetProperty("Assignee").ValueKind.ShouldBe(JsonValueKind.Null);
    }
}
