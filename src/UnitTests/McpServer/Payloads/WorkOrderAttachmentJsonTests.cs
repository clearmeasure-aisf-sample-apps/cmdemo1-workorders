using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Payloads;

[TestFixture]
public class WorkOrderAttachmentJsonTests
{
    [Test]
    public void Serialize_Should_WriteFileNameTypeAndSize()
    {
        WorkOrderAttachmentJson[] attachments = [new() { FileName = "leak.jpg", ContentType = "image/jpeg", FileSize = 2048 }];

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(attachments, McpJsonContext.Default.WorkOrderAttachmentJsonArray));

        var attachment = json.RootElement[0];
        attachment.GetProperty("FileName").GetString().ShouldBe("leak.jpg");
        attachment.GetProperty("ContentType").GetString().ShouldBe("image/jpeg");
        attachment.GetProperty("FileSize").GetInt64().ShouldBe(2048);
    }
}
