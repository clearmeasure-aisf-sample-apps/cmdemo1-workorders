using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools;

[TestFixture]
public class WorkOrderJsonTests
{
    [Test]
    public void ShouldWriteTheFullDetail_WhenOneWorkOrderIsAnswered()
    {
        var draft = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());

        using var json = JsonDocument.Parse(WorkOrderJson.Detail(draft));

        json.RootElement.GetProperty("Number").GetString().ShouldBe("WO-1");
        json.RootElement.GetProperty("Status").GetString().ShouldBe("Draft");
        json.RootElement.GetProperty("CreatorUsername").GetString().ShouldBe("jsmith");
        json.RootElement.GetProperty("DueDate").GetString().ShouldBe("2026-12-01");
        json.RootElement.GetProperty("Priority").GetString().ShouldBe("Medium");
    }

    [Test]
    public void ShouldWriteAnEmptyInstruction_WhenTheWorkOrderHasNone()
    {
        var draft = WorkOrderToolMother.Draft(WorkOrderToolMother.Creator());
        draft.Instructions = null;

        using var json = JsonDocument.Parse(WorkOrderJson.Detail(draft));

        json.RootElement.GetProperty("Instructions").GetString().ShouldBe("");
    }

    [Test]
    public void ShouldWriteOneIndentedSummaryPerWorkOrder_WhenAListIsAnswered()
    {
        var creator = WorkOrderToolMother.Creator();

        var text = WorkOrderJson.Summaries([WorkOrderToolMother.Draft(creator), WorkOrderToolMother.Assigned(creator)]);

        using var json = JsonDocument.Parse(text);
        json.RootElement.GetArrayLength().ShouldBe(2);
        json.RootElement[1].GetProperty("Status").GetString().ShouldBe("Assigned");
        json.RootElement[0].TryGetProperty("Description", out _).ShouldBeFalse();
        text.ShouldContain(Environment.NewLine);
    }

    [Test]
    public void ShouldNameTheUploader_WhenAttachmentsAreAnswered()
    {
        var attachment = new WorkOrderAttachment { FileName = "boiler.jpg", UploadedBy = WorkOrderToolMother.Creator() };

        using var json = JsonDocument.Parse(WorkOrderJson.Attachments([attachment]));

        json.RootElement[0].GetProperty("FileName").GetString().ShouldBe("boiler.jpg");
        json.RootElement[0].GetProperty("UploadedByUsername").GetString().ShouldBe("jsmith");
    }
}
