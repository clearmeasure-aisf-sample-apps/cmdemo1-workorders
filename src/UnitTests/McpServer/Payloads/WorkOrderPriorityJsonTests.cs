using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Payloads;

[TestFixture]
public class WorkOrderPriorityJsonTests
{
    [Test]
    public void Serialize_Should_WriteNameAndNumericValue()
    {
        WorkOrderPriorityJson[] priorities = [new("High", 2)];

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(priorities, McpJsonContext.Default.WorkOrderPriorityJsonArray));

        json.RootElement[0].GetProperty("Name").GetString().ShouldBe("High");
        json.RootElement[0].GetProperty("Value").GetInt32().ShouldBe(2);
    }
}
