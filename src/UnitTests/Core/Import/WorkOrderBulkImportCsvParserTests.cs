using System.Text;
using ClearMeasure.Bootcamp.Core.Import;
using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Import;

[TestFixture]
public class WorkOrderBulkImportCsvParserTests
{
    [Test]
    public void ShouldFail_WhenCsvIsEmpty()
    {
        using var ms = new MemoryStream();
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe("CSV is empty.");
    }

    [Test]
    public void ShouldFail_WhenRequiredColumnMissing()
    {
        using var ms = new MemoryStream("Title,Description\na,b\n"u8.ToArray());
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeFalse();
        result.Error!.ShouldContain("CreatorUsername");
    }

    [Test]
    public void ShouldParseRows_WhenHeaderAndDataPresent()
    {
        var csv = "Title,Description,CreatorUsername,RoomNumber\n"
                  + "Fix leak,Under sink,u1,101\n"
                  + "\"Title, with comma\",Plain desc,u2,\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(2);
        result.Rows[0].Title.ShouldBe("Fix leak");
        result.Rows[0].Description.ShouldBe("Under sink");
        result.Rows[0].CreatorUsername.ShouldBe("u1");
        result.Rows[0].RoomNumber.ShouldBe("101");
        result.Rows[1].Title.ShouldBe("Title, with comma");
        result.Rows[1].Description.ShouldBe("Plain desc");
        result.Rows[1].CreatorUsername.ShouldBe("u2");
        result.Rows[1].RoomNumber.ShouldBeNull();
    }

    [Test]
    public void ShouldUnescapeDoubledQuotes_WhenInsideQuotedField()
    {
        var csv = "Title,Description,CreatorUsername\n"
                  + "T,\"He said \"\"hi\"\"\",u\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);
        result.Rows[0].Description.ShouldBe("He said \"hi\"");
    }

    [Test]
    public void ShouldParseTwoDataRows_WhenMatchesIntegrationTestCsv()
    {
        var csv = "Title,Description,CreatorUsername,RoomNumber\n"
                  + "First,Desc one,bulk-user,1A\n"
                  + "Second,Desc two,bulk-user,\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(2);
    }

    [Test]
    public void ShouldParseMultilineQuotedField_WhenLogicalLineSpansRows()
    {
        var csv = "Title,Description,CreatorUsername\n"
                  + "T,\"Line one\nLine two\",u\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);
        result.Rows[0].Description.ShouldBe("Line one\nLine two");
    }

    [Test]
    public void ShouldSucceedWithEmptyRows_WhenHeaderOnly()
    {
        using var ms = new MemoryStream("Title,Description,CreatorUsername\n"u8.ToArray());
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldSkipBlankDataLines_WhenWhitespaceOnlyRowsPresent()
    {
        var csv = "Title,Description,CreatorUsername\n"
                  + "   \n"
                  + "Keep,Desc,user1\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);
        result.Rows[0].Title.ShouldBe("Keep");
    }
    private static WorkOrderBulkImportParseResult ParseCsv(string csv)
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        return WorkOrderBulkImportCsvParser.Parse(ms);
    }

    [Test]
    public void ShouldParseBuildingPriorityAndDueDate_WhenColumnsPresent()
    {
        var result = ParseCsv("Title,Description,CreatorUsername,RoomNumber,Building,Priority,DueDate\n"
                              + "T,D,u,1,North Hall,hIgH,2026-11-05\n");

        result.Success.ShouldBeTrue();
        var row = result.Rows[0];
        row.Building.ShouldBe("North Hall");
        row.Priority.ShouldBe(WorkOrderPriority.High);
        row.DueDate.ShouldBe(new DateOnly(2026, 11, 5));
        row.Error.ShouldBeNull();
    }

    [Test]
    public void ShouldDefaultNewColumnsToNull_WhenColumnsAbsent()
    {
        var result = ParseCsv("Title,Description,CreatorUsername\nT,D,u\n");

        var row = result.Rows[0];
        row.Building.ShouldBeNull();
        row.Priority.ShouldBeNull();
        row.DueDate.ShouldBeNull();
        row.Error.ShouldBeNull();
    }

    [Test]
    public void ShouldDefaultNewColumnsToNull_WhenCellsEmpty()
    {
        var result = ParseCsv("Title,Description,CreatorUsername,Building,Priority,DueDate\nT,D,u, , ,\n");

        var row = result.Rows[0];
        row.Building.ShouldBeNull();
        row.Priority.ShouldBeNull();
        row.DueDate.ShouldBeNull();
        row.Error.ShouldBeNull();
    }

    [TestCase("Urgent")]
    [TestCase("2")]
    [TestCase("-1")]
    public void ShouldReportError_WhenPriorityInvalid(string priority)
    {
        var result = ParseCsv($"Title,Description,CreatorUsername,Priority\nT,D,u,Low\nT,D,u,{priority}\n");

        result.Rows[0].Error.ShouldBeNull();
        result.Rows[1].LineNumber.ShouldBe(3);
        result.Rows[1].Error.ShouldBe($"Priority '{priority}' is not valid. Expected one of: Low, Medium, High.");
    }

    [TestCase("2026-13-01")]
    [TestCase("14/10/2026")]
    public void ShouldReportError_WhenDueDateMalformed(string dueDate)
    {
        var result = ParseCsv($"Title,Description,CreatorUsername,DueDate\nT,D,u,{dueDate}\n");

        result.Rows[0].LineNumber.ShouldBe(2);
        result.Rows[0].Error.ShouldBe($"DueDate '{dueDate}' is not a valid date (expected yyyy-MM-dd).");
    }

    [Test]
    public void ShouldReportError_WhenBuildingTooLong()
    {
        var building = new string('b', WorkOrder.BuildingMaxLength + 1);
        var result = ParseCsv($"Title,Description,CreatorUsername,Building\nT,D,u,ok\nT,D,u,{building}\n");

        result.Rows[0].Error.ShouldBeNull();
        result.Rows[1].LineNumber.ShouldBe(3);
        result.Rows[1].Error.ShouldBe(WorkOrderFieldRules.BuildingMustBeAtMost);
    }

    [Test]
    public void ShouldAcceptDescription_WhenAtMaxLength()
    {
        var description = new string('d', WorkOrder.DescriptionMaxLength);
        var result = ParseCsv($"Title,Description,CreatorUsername\nT,{description},u\n");

        result.Rows[0].Error.ShouldBeNull();
    }

    [Test]
    public void ShouldReportError_WhenDescriptionTooLong()
    {
        var description = new string('d', WorkOrder.DescriptionMaxLength + 1);
        var result = ParseCsv($"Title,Description,CreatorUsername\nT,ok,u\nT,{description},u\n");

        result.Rows[0].Error.ShouldBeNull();
        result.Rows[1].LineNumber.ShouldBe(3);
        result.Rows[1].Error.ShouldBe(WorkOrderFieldRules.DescriptionCannotExceed);
    }

    [Test]
    public void ShouldListOptionalColumns_WhenRequiredColumnMissing()
    {
        var result = ParseCsv("Title,Description\na,b\n");

        result.Error.ShouldBe("Missing required column \"CreatorUsername\". Expected header: Title, Description, "
                              + "CreatorUsername, RoomNumber (optional), Building (optional), Priority (optional), DueDate (optional).");
    }

    [TestCase("high", WorkOrderPriority.High)]
    [TestCase("HIGH", WorkOrderPriority.High)]
    [TestCase("Low", WorkOrderPriority.Low)]
    [TestCase("mEdIuM", WorkOrderPriority.Medium)]
    public void ShouldParsePriority_WhenNameCasingDiffers(string priority, WorkOrderPriority expected)
    {
        var result = ParseCsv($"Title,Description,CreatorUsername,Priority\nT,D,u,{priority}\n");

        result.Rows[0].Priority.ShouldBe(expected);
        result.Rows[0].Error.ShouldBeNull();
    }

    [TestCase("1")]
    [TestCase("3")]
    public void ShouldReportError_WhenPriorityIsNumericValueOfDefinedMember(string priority)
    {
        var result = ParseCsv($"Title,Description,CreatorUsername,Priority\nT,D,u,{priority}\n");

        result.Rows[0].Priority.ShouldBeNull();
        result.Rows[0].Error.ShouldBe($"Priority '{priority}' is not valid. Expected one of: Low, Medium, High.");
    }

    [Test]
    public void ShouldParseRows_WhenHeaderIsReorderedDifferentlyCasedAndHasUnknownColumn()
    {
        var result = ParseCsv("creatorusername,Extra,TITLE,Description\nu1,ignored,T,D\n");

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);
        result.Rows[0].Title.ShouldBe("T");
        result.Rows[0].Description.ShouldBe("D");
        result.Rows[0].CreatorUsername.ShouldBe("u1");
        result.Rows[0].RoomNumber.ShouldBeNull();
        result.Rows[0].Error.ShouldBeNull();
    }

    [Test]
    public void ShouldParseHeader_WhenUtf8BomPrecedesIt()
    {
        var csv = Encoding.UTF8.GetBytes("Title,Description,CreatorUsername\nT,D,u\n");
        using var ms = new MemoryStream([.. Encoding.UTF8.GetPreamble(), .. csv]);
        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);
        result.Rows[0].Title.ShouldBe("T");
        result.Rows[0].CreatorUsername.ShouldBe("u");
    }

    private static readonly string TooLongBuilding = new('b', WorkOrder.BuildingMaxLength + 1);
    private static readonly string TooLongDescription = new('d', WorkOrder.DescriptionMaxLength + 1);
    private const string InvalidPriorityError = "Priority 'Urgent' is not valid. Expected one of: Low, Medium, High.";
    private const string InvalidDueDateError = "DueDate '2026-13-01' is not a valid date (expected yyyy-MM-dd).";

    private static IEnumerable<TestCaseData> RowsWithSeveralInvalidCells() =>
    [
        new TestCaseData(new[] { TooLongBuilding, TooLongDescription, "Urgent", "2026-13-01" }, WorkOrderFieldRules.BuildingMustBeAtMost),
        new TestCaseData(new[] { "North Hall", TooLongDescription, "Urgent", "2026-13-01" }, WorkOrderFieldRules.DescriptionCannotExceed),
        new TestCaseData(new[] { "North Hall", "ok", "Urgent", "2026-13-01" }, InvalidPriorityError),
        new TestCaseData(new[] { "North Hall", "ok", "High", "2026-13-01" }, InvalidDueDateError),
    ];

    [TestCaseSource(nameof(RowsWithSeveralInvalidCells))]
    public void ShouldReportOnlyFirstError_WhenSeveralCellsInvalidInBuildingDescriptionPriorityDueDateOrder(
        string[] buildingDescriptionPriorityDueDate, string expectedError)
    {
        var cells = string.Join(',', Enumerable.Reverse(buildingDescriptionPriorityDueDate));

        var result = ParseCsv($"Title,CreatorUsername,DueDate,Priority,Description,Building\nT,u,{cells}\n");

        result.Rows[0].Error.ShouldBe(expectedError);
    }

    [Test]
    public void ShouldReportLastPhysicalLineNumber_WhenRowFollowsMultilineQuotedRecord()
    {
        var result = ParseCsv("Title,Description,CreatorUsername\nA,\"Line one\nLine two\",u\nB,D,u\n");

        result.Rows.Count.ShouldBe(2);
        result.Rows[0].LineNumber.ShouldBe(3);
        result.Rows[1].LineNumber.ShouldBe(4);
    }
}
